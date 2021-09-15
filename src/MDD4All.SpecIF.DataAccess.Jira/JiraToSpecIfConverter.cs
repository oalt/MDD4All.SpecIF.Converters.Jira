using Jira3 = MDD4All.Jira.DataModels.V3;
using MDD4All.SpecIF.DataModels;
using MDD4All.SpecIF.DataProvider.Contracts;
using MDD4All.SpecIF.DataModels.Manipulation;
using MDD4All.SpecIF.DataModels.Helpers;
using System.Collections.Generic;
using MDD4All.SpecIF.DataFactory;
using Newtonsoft.Json.Linq;

namespace MDD4All.SpecIF.DataAccess.Jira
{
    public class JiraToSpecIfConverter
    {
        private string _user;

        private ISpecIfMetadataReader _metadataReader;

        private List<Jira3.Status> _statusInformation;

        public JiraToSpecIfConverter(ISpecIfMetadataReader metadataReader, List<Jira3.Status> statusInformation)
        {
            _metadataReader = metadataReader;
            _statusInformation = statusInformation;
        }

        public Resource ConvertToResource(Jira3.Issue jiraIssue)
        {
            Resource result = null;

            Key classKey = new Key("RC-Requirement", "1.1");

            result = SpecIfDataFactory.CreateResource(classKey, _metadataReader);

            if (jiraIssue.Fields.IssueType.Name == "Requirement")
            {
                classKey = new Key("RC-Requirement", "1.1");
                result.SetPropertyValue("SpecIF:Perspective", "V-perspective-2", _metadataReader);
            }
            else if (jiraIssue.Fields.IssueType.Name == "Customer Requirement")
            {
                classKey = new Key("RC-Requirement", "1.1");
                result.SetPropertyValue("SpecIF:Perspective", "V-perspective-1", _metadataReader);
            }

            // EA GUID
            string eaGuidFieldName = GetEaGuidFieldName(jiraIssue);

            string eaGuid = (string)jiraIssue.FieldDictionary[eaGuidFieldName];

            if (!string.IsNullOrEmpty(eaGuid))
            {
                AlternativeId alternativeId = new AlternativeId()
                {
                    ID = eaGuid,
                    Project = "Enterprise Architect"
                };

                result.AlternativeIDs.Add(alternativeId);
            }

            string specIfGuid = JiraGuidConverter.ConvertToSpecIfGuid(jiraIssue.Self, jiraIssue.ID);

            result.ID = specIfGuid;

            result.Revision = SpecIfGuidGenerator.ConvertDateToRevision(jiraIssue.Fields.Updated.Value);

            result.ChangedAt = jiraIssue.Fields.Updated.Value;

            if(jiraIssue.ChangeLog.Total == 0)
            { 
                result.ChangedBy = jiraIssue.Fields.Creator.DisplayName;
            }
            else
            {
                result.ChangedBy = jiraIssue.ChangeLog.Histories[0].Author.DisplayName;
            }

            //result.Title = jiraIssue.Fields.Summary;

            if(jiraIssue.ChangeLog.Total > 1)
            {
                Jira3.History predecessor = jiraIssue.ChangeLog.Histories[1];

                string preRevision = SpecIfGuidGenerator.ConvertDateToRevision(predecessor.Created);

                result.Replaces.Add(preRevision);
            }

            result.SetPropertyValue("dcterms:identifier", jiraIssue.Key, _metadataReader);

            

            AdfToXhtmlConverter adfToXhtmlConverter = new AdfToXhtmlConverter();

            string descriptionHtml = adfToXhtmlConverter.ConvertAdfToXhtml(jiraIssue.Fields.Description);

            

            string mainLanguage = "en";

            string languageFieldID = GetCustomFieldName(jiraIssue, "Language");
            if (!string.IsNullOrEmpty(languageFieldID))
            {
                JObject language = jiraIssue.FieldDictionary[languageFieldID] as JObject;

                if (language != null)
                {
                    string languageValue = (string)language["value"];

                    if (!string.IsNullOrEmpty(languageValue))
                    {
                        if (languageValue.ToLower().StartsWith("de"))
                        {
                            mainLanguage = "de";
                        }
                        else if (languageValue.ToLower().StartsWith("en"))
                        {
                            mainLanguage = "en";
                        }
                        else if (languageValue.ToLower().StartsWith("zh"))
                        {
                            mainLanguage = "zh";
                        }
                    }
                }
            }

            string secondLanguage = "de";
            
            string foreignLanguageFieldID = GetCustomFieldName(jiraIssue, "Foreign Language");
            if (!string.IsNullOrEmpty(foreignLanguageFieldID))
            {
                JObject foreignLanguage = jiraIssue.FieldDictionary[foreignLanguageFieldID] as JObject;
                if (foreignLanguage != null)
                {
                    string foreignLanguageValue = (string)foreignLanguage["value"];

                    if (!string.IsNullOrEmpty(foreignLanguageValue))
                    {

                        if (foreignLanguageValue.ToLower().StartsWith("en"))
                        {
                            secondLanguage = "en";
                        }
                        else if (foreignLanguageValue.ToLower().StartsWith("de"))
                        {
                            secondLanguage = "de";
                        }
                        else if (foreignLanguageValue.ToLower().StartsWith("zh"))
                        {
                            secondLanguage = "zh";
                        }
                    }
                }
            }

            if(mainLanguage == secondLanguage)
            {
                if(mainLanguage == "en")
                {
                    secondLanguage = "de";
                }
                else if(mainLanguage == "de")
                {
                    secondLanguage = "en";
                }
                else
                {
                    mainLanguage = "en";
                }
            }

            string mainTitleContent = jiraIssue.Fields.Summary;
            string mainDescriptionContent = descriptionHtml;

            string secondTitleFieldID = GetCustomFieldName(jiraIssue, "Foreign Title");
            string secondTitleContent = "";

            if(!string.IsNullOrEmpty(secondTitleFieldID))
            {
                secondTitleContent = (string)jiraIssue.FieldDictionary[secondTitleFieldID];
            }

            string secondDescriptionFieldID = GetCustomFieldName(jiraIssue, "Foreign Description");
            string secondDescriptionContent = "";

            if(!string.IsNullOrEmpty(secondDescriptionFieldID))
            {
                JObject secondDescriptionJobject = jiraIssue.FieldDictionary[secondDescriptionFieldID] as JObject;

                if (secondDescriptionJobject != null)
                {
                    Jira3.ADF.AtlassianDocumentFormat secondDescriptionADF = secondDescriptionJobject.ToObject<Jira3.ADF.AtlassianDocumentFormat>();

                    secondDescriptionContent = adfToXhtmlConverter.ConvertAdfToXhtml(secondDescriptionADF);
                }
            }

            Value titleValue = new Value();
            Value descriptionValue = new Value();

            if (mainLanguage == "en")
            {
                MultilanguageText firstTitle = new MultilanguageText
                {
                    Text = mainTitleContent,
                    Language = mainLanguage,
                    Format = TextFormat.Plain
                };

                titleValue.MultilanguageText.Add(firstTitle);

                MultilanguageText secondTitle = new MultilanguageText
                {
                    Text = secondTitleContent,
                    Language = secondLanguage,
                    Format = TextFormat.Plain
                };

                titleValue.MultilanguageText.Add(secondTitle);

                MultilanguageText firstDescription = new MultilanguageText
                {
                    Text = mainDescriptionContent,
                    Language = mainLanguage,
                    Format = TextFormat.XHTML
                };

                descriptionValue.MultilanguageText.Add(firstDescription);

                MultilanguageText secondDescription = new MultilanguageText
                {
                    Text = secondDescriptionContent,
                    Language = secondLanguage,
                    Format = TextFormat.XHTML
                };

                descriptionValue.MultilanguageText.Add(secondDescription);

            }
            else if(secondLanguage == "en")
            {
                MultilanguageText firstTitle = new MultilanguageText
                {
                    Text = secondTitleContent,
                    Language = secondLanguage,
                    Format = TextFormat.Plain
                };

                titleValue.MultilanguageText.Add(firstTitle);

                MultilanguageText secondTitle = new MultilanguageText
                {
                    Text = mainTitleContent,
                    Language = mainLanguage,
                    Format = TextFormat.Plain
                };

                titleValue.MultilanguageText.Add(secondTitle);

                MultilanguageText firstDescription = new MultilanguageText
                {
                    Text = secondDescriptionContent,
                    Language = "en",
                    Format = TextFormat.XHTML
                };

                descriptionValue.MultilanguageText.Add(firstDescription);

                MultilanguageText secondDescription = new MultilanguageText
                {
                    Text = mainDescriptionContent,
                    Language = mainLanguage,
                    Format = TextFormat.XHTML
                };

                descriptionValue.MultilanguageText.Add(secondDescription);
            }

            result.SetPropertyValue("dcterms:title", titleValue, _metadataReader);

            //jiraIssue.FieldDictionary[""];

            result.SetPropertyValue("dcterms:description", descriptionValue, _metadataReader);

            string lifecycleStatus = ConvertJiraStatusToSpecIfLifeCycleStatus(jiraIssue);
            result.SetPropertyValue("SpecIF:LifeCycleStatus", lifecycleStatus, _metadataReader);

            return result;
        }

        private string ConvertJiraStatusToSpecIfLifeCycleStatus(Jira3.Issue jiraIssue)
        {
            string result = "";

            Jira3.Status status = jiraIssue.Fields.Status;

            string statusValue = status.Name.ToLowerInvariant();

            switch(statusValue)
            {
                case "drafted":
                    result = "V-Status-3";
                    break;

                case "rejected":
                    result = "V-Status-1";
                    break;

                case "deprecated":
                    result = "V-Status-0";
                    break;

                case "submitted":
                    result = "V-Status-4";
                    break;

                case "approved":
                    result = "V-Status-5";
                    break;

                case "completed":
                    result = "V-Status-6";
                    break;

                case "verified":
                    result = "V-Status-9";
                    break;

                case "released":
                    result = "V-Status-7";
                    break;
            }

            return result;
        }

        private string GetEaGuidFieldName(Jira3.Issue issue)
        {
            string result = "";

            if (issue.FieldNames != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in issue.FieldNames)
                {
                    if(keyValuePair.Value == "EA GUID")
                    {
                        result = keyValuePair.Key;
                        break;
                    }
                }
            }

            return result;
        }

        private string GetCustomFieldName(Jira3.Issue issue, string customFieldTitle)
        {
            string result = "";

            if (issue.FieldNames != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in issue.FieldNames)
                {
                    if (keyValuePair.Value == customFieldTitle)
                    {
                        result = keyValuePair.Key;
                        break;
                    }
                }
            }

            return result;
        }

       


        public Resource ConvertToResource(Jira3.JiraWebhookObject jiraWebhookObject)
        {
            Resource result;

            _user = jiraWebhookObject.User.DisplayName;

            result = ConvertToResource(jiraWebhookObject.Issue);

            return result;
        }

        
    }
}
