using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace BDORootBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly BotFrameworkAuthentication _auth;
        private readonly string _connectionName;
        private readonly BotFrameworkSkill _ssoSkill;
        private readonly IConnectorClient _connectorClient;
        public MainDialog(IConnectorClient connectorClient)
        {
            _connectorClient = connectorClient;
        }
        public MainDialog(BotFrameworkAuthentication auth, ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillsConfiguration skillsConfig, IConfiguration configuration)
            : base(nameof(MainDialog))
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));

            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentException($"{MicrosoftAppCredentials.MicrosoftAppIdKey} is not set in configuration");
            }

            _connectionName = configuration.GetSection("ConnectionName")?.Value;
            if (string.IsNullOrWhiteSpace(_connectionName))
            {
                throw new ArgumentException("\"ConnectionName\" is not set in configuration");
            }

            // We use a single skill in this example.
            var targetSkillId = "SkillBot";
            if (!skillsConfig.Skills.TryGetValue(targetSkillId, out _ssoSkill))
            {
                throw new ArgumentException($"Skill with ID \"{targetSkillId}\" not found in configuration");
            }

            AddDialog(new ChoicePrompt("ActionStepPrompt"));
            AddDialog(new SkillDialog(CreateSkillDialogOptions(skillsConfig, botId, conversationIdFactory, conversationState), nameof(SkillDialog)));

            var waterfallSteps = new WaterfallStep[]
            {

              CallConnetAgentAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Helper to create a SkillDialogOptions instance for the SSO skill.
        private SkillDialogOptions CreateSkillDialogOptions(SkillsConfiguration skillsConfig, string botId, SkillConversationIdFactoryBase conversationIdFactory, ConversationState conversationState)
        {
            return new SkillDialogOptions
            {
                BotId = botId,
                ConversationIdFactory = conversationIdFactory,
                SkillClient = _auth.CreateBotFrameworkClient(),
                SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
                ConversationState = conversationState,
                Skill = _ssoSkill,
            };
        }

        private async Task<DialogTurnResult> CallConnetAgentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var userMessage = stepContext.Context.Activity.Text?.ToLowerInvariant();
            if (userMessage == null)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (userMessage == "hi" || userMessage == "help" || userMessage == "need help" || userMessage == "having a issue" || userMessage == "issue" || userMessage == "hi bcc" ||
                          userMessage == "hi it support" || userMessage == "hello")
            {

                var beginSkillActivity = new Activity
                {
                    Type = ActivityTypes.Event,
                    Name = "SSO"
                };

                // Save active skill in state (this is use in case of errors in the AdapterWithErrorHandler).
                await _activeSkillProperty.SetAsync(stepContext.Context, _ssoSkill, cancellationToken);
                var typingActivity = new Activity
                {
                    Type = ActivityTypes.Typing,
                };
                await stepContext.Context.SendActivityAsync(typingActivity);
                return await stepContext.BeginDialogAsync(nameof(SkillDialog), new BeginSkillDialogOptions { Activity = beginSkillActivity }, cancellationToken);
            }
            else
            {

                await stepContext.Context.SendActivityAsync("Sorry! I didn't understand you. Did you mean 'Hi'?");


                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

        }

    }
}
