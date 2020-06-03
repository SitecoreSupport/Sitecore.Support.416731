namespace Sitecore.Support.EmailCampaing.Cm
{
    using Sitecore.Diagnostics;
    using Sitecore.EmailCampaign.Model.Web.Settings;
    using Sitecore.Framework.Conditions;
    using Sitecore.Marketing.Core.Extensions;
    using Sitecore.Modules.EmailCampaign;
    using Sitecore.Modules.EmailCampaign.Core.Pipelines.GenerateLink;
    using Sitecore.Modules.EmailCampaign.Messages;
    using Sitecore.EmailCampaign.Cm.Factories;
    using Sitecore.Web;
    using Sitecore.XConnect;
    using System;

    public class SubscriptionManager : Sitecore.EmailCampaign.Cm.SubscriptionManager
    {
        private readonly Sitecore.Modules.EmailCampaign.Core.PipelineHelper _pipelineHelper;

        private readonly Sitecore.Modules.EmailCampaign.Core.HostnameMapping.IHostnameMappingService _hostnameMappingService;

        private readonly Sitecore.Modules.EmailCampaign.Core.Contacts.IContactService _contactService;

        public SubscriptionManager(Sitecore.Modules.EmailCampaign.Core.Contacts.IContactService contactService, Sitecore.ExM.Framework.Diagnostics.ILogger logger,
            Sitecore.Modules.EmailCampaign.ListManager.ListManagerWrapper listManagerWrapper, Sitecore.Modules.EmailCampaign.Services.IExmCampaignService exmCampaignService,
            Sitecore.Modules.EmailCampaign.Core.PipelineHelper pipelineHelper, ISendingManagerFactory sendingManagerFactory, Sitecore.Modules.EmailCampaign.Services.IManagerRootService managerRootService,
            Sitecore.Modules.EmailCampaign.Factories.IRecipientManagerFactory recipientManagerFactory, Sitecore.Modules.EmailCampaign.Core.HostnameMapping.IHostnameMappingService hostnameMappingService)
            : base(contactService, logger, listManagerWrapper, exmCampaignService, pipelineHelper, sendingManagerFactory, managerRootService, recipientManagerFactory)
        {
            Assert.ArgumentNotNull(hostnameMappingService, "hostnameMappingService");
            Assert.ArgumentNotNull(contactService, "contactService");
            Assert.ArgumentNotNull(exmCampaignService, "exmCampaignService");
            _hostnameMappingService = hostnameMappingService;
            _contactService = contactService;
            _pipelineHelper = pipelineHelper;
        }

        public override bool SendConfirmationMessage(Contact contact, Guid recipientListId, ManagerRoot managerRoot)
        {
            Assert.ArgumentNotNull(contact, "contact");
            Condition.Requires(recipientListId, "recipientListId").IsNotEmptyGuid();
            if (managerRoot == null)
            {
                return false;
            }
            MessageItem subscriptionConfirmationMessage = managerRoot.Settings.SubscriptionConfirmationMessage;
            string serverUrl = managerRoot.Settings.BaseURL;
            string str = serverUrl + "/sitecore%20modules/Web/EXM/ConfirmSubscription.aspx?";
            str = str + GlobalSettings.ConfirmSubscriptionQueryStringKey + "=" + GetConfirmationKey(recipientListId, _contactService.GetIdentifier(contact), managerRoot);
            MailMessageItem mailMessageItem = subscriptionConfirmationMessage as MailMessageItem;
            if (mailMessageItem != null)
            {
                mailMessageItem.ContactIdentifier = _contactService.GetIdentifier(contact);
                GenerateLinkPipelineArgs generateLinkPipelineArgs = new GenerateLinkPipelineArgs(str, mailMessageItem, previewMode: false);
                _pipelineHelper.RunPipeline("modifyHyperlink", generateLinkPipelineArgs);
                if (!generateLinkPipelineArgs.Aborted && !string.IsNullOrEmpty(generateLinkPipelineArgs.GeneratedUrl))
                {
                    str = generateLinkPipelineArgs.GeneratedUrl;
                }
            }
            subscriptionConfirmationMessage.CustomPersonTokens["link"] = str;
            return SendInfoMessage(subscriptionConfirmationMessage, contact);
        }
    }
}