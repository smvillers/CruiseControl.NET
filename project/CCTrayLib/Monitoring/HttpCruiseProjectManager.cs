using System;
using System.Collections.Specialized;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Remote;
using System.Collections.Generic;
using ThoughtWorks.CruiseControl.Remote.Parameters;
using System.Xml;

namespace ThoughtWorks.CruiseControl.CCTrayLib.Monitoring
{
	public class HttpCruiseProjectManager : ICruiseProjectManager
	{
		private readonly string projectName;
		private readonly IWebRetriever webRetriever;
		private readonly ICruiseServerManager serverManager;
		private Uri dashboardUri;
        private Uri parametersUri;
		private Uri webUrl;
		private string serverAlias = "local";

		public HttpCruiseProjectManager(IWebRetriever webRetriever, string projectName, ICruiseServerManager serverManager)
		{
			this.projectName = projectName;
			this.webRetriever = webRetriever;
			this.serverManager = serverManager;
		}

        public void ForceBuild(string sessionToken, Dictionary<string, string> parameters)
		{
			PushDashboardButton(sessionToken, "ForceBuild");
		}

        public void AbortBuild(string sessionToken)
		{
			PushDashboardButton(sessionToken, "AbortBuild");
		}

		public void FixBuild(string sessionToken, string fixingUserName)
		{
			throw new NotImplementedException("Fix build not currently supported on projects monitored via HTTP");
		}

        public void StopProject(string sessionToken)
		{
			PushDashboardButton(sessionToken, "StopBuild");
		}

        public void StartProject(string sessionToken)
		{
			PushDashboardButton(sessionToken, "StartBuild");
		}

        public void CancelPendingRequest(string sessionToken)
		{
			throw new NotImplementedException("Cancel pending not currently supported on projects monitored via HTTP");
		}

		public string ProjectName
		{
			get { return projectName; }
		}

		public void PushDashboardButton(string sessionToken, string buttonName)
		{
            PushDashboardButton(sessionToken, buttonName, null);
        }

		public void PushDashboardButton(string sessionToken, string buttonName, Dictionary<string, string> parameters)
		{
			try
			{
				InitConnection();
				NameValueCollection input = new NameValueCollection();
				input.Add(buttonName, "true");
				input.Add("projectName", projectName);
				input.Add("serverName", serverAlias);
                input.Add("sessionToken", sessionToken);
                if (parameters != null)
                {
                    foreach (string key in parameters.Keys)
                    {
                        input.Add("param_" + key, parameters[key]);
                    }
                }
                string response = webRetriever.Post(dashboardUri, input);

                // The dashboard catches and handles all exceptions, these exceptions need to be passed on
                HandleResponseAndThrowExceptions(response);
			}
			// Silently ignore exceptions that occur due to connection problems
			catch (System.Net.WebException)
			{
			}
            // Catch any permission denied exceptions and pass in the correct permission
            catch (PermissionDeniedException)
            {
                throw new PermissionDeniedException(buttonName);
            }
		}

        private void HandleResponseAndThrowExceptions(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                if (response.Contains("ThoughtWorks.CruiseControl.Core.SessionInvalidException"))
                {
                    throw new SessionInvalidException();
                }
                else if (response.Contains("ThoughtWorks.CruiseControl.Core.PermissionDeniedException"))
                {
                    throw new PermissionDeniedException("Unknown");
                }
                else if (response.Contains("ThoughtWorks.CruiseControl.Core.SecurityException"))
                {
                    throw new SecurityException();
                }
                else if (response.Contains("ThoughtWorks.CruiseControl.Core.NoSuchProjectException"))
                {
                    throw new NoSuchProjectException();
                }
            }
		}

		private void InitConnection()
		{
			ProjectStatus ps = serverManager.GetCruiseServerSnapshot().GetProjectStatus(projectName);
			if (ps != null)
			{
				webUrl = new Uri(ps.WebURL);
				ExtractServerAlias();
			}
            dashboardUri = new Uri(new WebDashboardUrl(serverManager.Configuration.Url, serverAlias).ViewFarmReport);
            parametersUri = new Uri(new WebDashboardUrl(serverManager.Configuration.Url, serverAlias).ViewParametersReport(projectName));
		}

		private void ExtractServerAlias()
		{
			string[] splitPath = new string[0];

			if (webUrl != null)
				splitPath = webUrl.AbsolutePath.Trim('/').Split('/');

			for (int i = 0; i < splitPath.Length; i++)
			{
				if ((splitPath[i] == "server") && (splitPath[i + 1] != null) && (splitPath[i + 1] != string.Empty))
				{
					serverAlias = splitPath[i + 1];
					break;
				}
			}
		}

        #region RetrieveSnapshot()
        /// <summary>
        /// Retrieves a snapshot of the current build status.
        /// </summary>
        /// <returns>The current build status of the project.</returns>
        public virtual ProjectStatusSnapshot RetrieveSnapshot()
        {
            ProjectStatusSnapshot snapshot = new ProjectStatusSnapshot();
            snapshot.Name = projectName;
            snapshot.Status = ItemBuildStatus.Unknown;
            return snapshot;
        }
        #endregion

        #region RetrievePackageList()
        /// <summary>
        /// Retrieves the current list of available packages.
        /// </summary>
        /// <returns></returns>
        public virtual PackageDetails[] RetrievePackageList()
        {
            PackageDetails[] list = new PackageDetails[0];
            return list;
        }
        #endregion

        #region RetrieveFileTransfer()
        /// <summary>
        /// Retrieve a file transfer object.
        /// </summary>
        /// <param name="project">The project to retrieve the file for.</param>
        /// <param name="fileName">The name of the file.</param>
        public virtual IFileTransfer RetrieveFileTransfer(string fileName)
        {
            throw new InvalidOperationException();
        }
        #endregion

        /// <summary>
        /// Retrieves any build parameters.
        /// </summary>
        /// <returns></returns>
        public virtual List<ParameterBase> ListBuildParameters()
        {
            InitConnection();

            string response = webRetriever.Post(parametersUri, new NameValueCollection());
            XmlDocument document = new XmlDocument();
            document.LoadXml(response);
            List<ParameterBase> results = new List<ParameterBase>();
            foreach (XmlElement paramNode in document.SelectNodes("//parameter"))
            {
                XmlNodeList values = paramNode.SelectNodes("value");
                if (values.Count > 0)
                {
                    SelectParameter parameter = new SelectParameter();
                    var allowedValues = new List<NameValuePair>();
                    foreach (XmlElement value in values)
                    {
                        allowedValues.Add(new NameValuePair(null, value.InnerText));
                    }
                    parameter.DataValues = allowedValues.ToArray();
                    parameter.Name = paramNode.GetAttribute("name");
                    parameter.DisplayName = paramNode.GetAttribute("displayName");
                    parameter.Description = paramNode.GetAttribute("description");
                    parameter.ClientDefaultValue = paramNode.GetAttribute("defaultValue");
                    results.Add(parameter);
                }
                else
                {
                    TextParameter parameter = new TextParameter();
                    parameter.Name = paramNode.GetAttribute("name");
                    parameter.DisplayName = paramNode.GetAttribute("displayName");
                    parameter.Description = paramNode.GetAttribute("description");
                    parameter.ClientDefaultValue = paramNode.GetAttribute("defaultValue");
                    results.Add(parameter);
                }
            }

            return results;
        }
	}
}
