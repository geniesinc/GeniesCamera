using System.Collections.Generic;

namespace Genies.FeatureFlags
{

    [FeatureFlagsContainer(-1000)]
    public static class SharedFeatureFlags
    {
        public const string NONE = "none";
        public const string BypassAuth = "bypass_auth";
        public const string ForceUpgrade = "force_upgrade";
        public const string FreeTexturePlacement = "free-texture-placement";
        public const string DynamicContent = "composer_dynamic_content";
        public const string GearContent = "gear_content";
        public const string ExternalGearContent = "external_gear_content";
        public const string ExternalThingsContent = "external_things_content";
        public const string ExternalSubSpeciesContent = "external_subspecies_content";
        public const string SilverStudioAssetFiltering = "silver_studio_asset_filtering";
        public const string DisableNfts = "disable_nfts";
        public const string BaserowCms = "baserow_cms";
        public const string AddressablesCmsLocationService = "addressables_cms_location_service";
        public const string AddressablesInventoryLocations = "addressables_inventory_locations";
        public const string UniversalContentLocations = "universal_content_locations";
        public const string LanguageSupport = "language_support";
        public const string DynamicConfigsFromApi = "dynamic_configs_from_api";
        public const string ExperiencesLauncher = "experiences_launcher";
        public const string Bugsee = "bugsee";
        public const string Friends = "friends";
        public const string FriendsFeed = "friends_feed";
        public const string SpotifyService = "spotify_service";
        public const string MoodAI = "mood_ai";
        public const string SpacesFeedFreeScrolling = "spaces_feed_freescrolling";
        public const string ContactsSync = "contacts_sync";
        public const string TraitArchetypes = "trait_archetypes";
        public const string GameLauncher = "game_launcher";
        public const string NonUmaAvatar = "non_uma_avatar";
        public const string FeedPrototyping = "feed_prototyping";
        public const string AIGCFlow = "aigc_flow";
        public const string DailyQuests = "daily_quests";
        public const string Marketplace = "marketplace";
        public const string GapAvatars = "gap_avatars";
        public const string LLMPhotoPersona = "llm_photo_persona";
        public const string LLMPhotoPersonaV2 = "llm_photo_persona_v2";
        public const string Chat_V2 = "chat_v2";
        public const string InventoryClient = "inventory_client";
        public const string SmartAvatar = "smart_avatar";
        public const string InAppPurchases = "in_app_purchases";
        public const string FirebasePushNotification = "firebase_push_notification";
        public const string PrePromptAigcDecor = "preprompt_aigc_decor";
        public const string SpacesEditing = "spaces_editing";
        public const string GeniesCameraDeepLink = "is_gc_deeplink_enabled";
        public const string SpacesWallEditing = "spaces_wall_editing";
        public const string RecommendationSystem = "is_preprompt_recommendation_enabled";
        public const string Onboarding_v1_5 = "onboarding_v1.5";
        public const string IsFeedHidden = "is_feed_hidden";
        public const string Chat_V3 = "chat_v3";
        public const string IsVoiceUIUXEnabled = "is_voice_uiux_enabled";

        private static List<string> _featureFlagIds = new List<string>()
        {
            BypassAuth,
            ForceUpgrade,
            FreeTexturePlacement,
            DynamicContent,
            GearContent,
            ExternalGearContent,
            ExternalThingsContent,
            ExternalSubSpeciesContent,
            SilverStudioAssetFiltering,
            DisableNfts,
            BaserowCms,
            AddressablesCmsLocationService,
            AddressablesInventoryLocations,
            LanguageSupport,
            DynamicConfigsFromApi,
            ExperiencesLauncher,
            Bugsee,
            Friends,
            FriendsFeed,
            SpotifyService,
            MoodAI,
            SpacesFeedFreeScrolling,
            ContactsSync,
            TraitArchetypes,
            GameLauncher,
            NonUmaAvatar,
            FeedPrototyping,
            AIGCFlow,
            DailyQuests,
            Marketplace,
            GapAvatars,
            LLMPhotoPersona,
            LLMPhotoPersonaV2,
            ExperiencesLauncher,
            InventoryClient,
            SmartAvatar,
            InAppPurchases,
            FirebasePushNotification,
            PrePromptAigcDecor,
            SpacesEditing,
            Chat_V2,
            GeniesCameraDeepLink,
            SpacesWallEditing,
            RecommendationSystem,
            Onboarding_v1_5,
            IsFeedHidden,
            Chat_V3,
            IsVoiceUIUXEnabled
        };

        public static List<string> GetList()
        {
            return _featureFlagIds;
        }
    }
}
