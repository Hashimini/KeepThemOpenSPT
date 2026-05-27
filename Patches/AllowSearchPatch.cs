using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KEEPTHEMOPEN.Patches
{
    public class AllowSearchPatch : ModulePatch
    {
        // Variables to let the reflection work
        // Heavily inspired on https://github.com/DrakiaXYZ/SPT-SearchOpenContainers/blob/master/Patches/ContainerMenuPatch.cs TY Drakia! <3
        private static Type PlayerActionClassType;
        private static Type MenuClassType;
        private static Type MenuItemClassType;
        private static Type StringLocalizeType;

        private static FieldInfo MenuItemNameField;
        private static FieldInfo MenuItemActionField;
        private static FieldInfo MenuActionsField;

        private static MethodInfo ContainerSearchMethod;
        private static MethodInfo LocalizedMethod;

        protected override MethodBase GetTargetMethod()
        {
            // Find EFT public classes
            PlayerActionClassType = PatchConstants.EftTypes.Single( x => x.GetMethods().Any(method => method.Name == "GetAvailableActions") );

            MenuClassType = PatchConstants.EftTypes.Single( x => x.GetMethod("SelectNextAction") != null );

            MenuItemClassType = PatchConstants.EftTypes.Single( x => x.GetField("TargetName") != null && x.GetField("Disabled") != null );

            StringLocalizeType = PatchConstants.EftTypes.Single( x => x.GetMethod("LocalizeAreaName") != null );

            // Get Menu options
            MenuItemNameField = AccessTools.Field(MenuItemClassType, "Name");
            MenuItemActionField = AccessTools.Field(MenuItemClassType, "Action");
            MenuActionsField = AccessTools.Field(MenuClassType, "Actions");

            // Find search method
            ContainerSearchMethod = AccessTools.GetDeclaredMethods(PlayerActionClassType)
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();

                    return parameters.Length == 4 && parameters[0].Name == "owner" && parameters[2].Name == "lootableContainer";
                });

            // Suport for other languages (Localization)
            LocalizedMethod = AccessTools.Method(
                StringLocalizeType,
                "Localized",
                new Type[] { typeof(string), typeof(string) }
            );

            return AccessTools.GetDeclaredMethods(PlayerActionClassType)
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();

                    return parameters.Length == 2
                        && parameters[0].Name == "owner"
                        && parameters[1].Name == "container";
                });
        }

        [PatchPostfix]
        public static void PatchPostfix( ref object __result, GamePlayerOwner owner, LootableContainer container)
        {
            // Keeps the mod away when doesnt need to work
            if (__result == null)
                return;

            if (container == null)
                return;

            if (container.DoorState != EDoorState.Open)
                return;

            IList MenuItems = MenuActionsField.GetValue(__result) as IList;

            if (MenuItems == null)
                return;

            bool hasSearchButton = false;

            foreach (object item in MenuItems)
            {
                object existingName = MenuItemNameField.GetValue(item);

                if (existingName == null)
                    continue;

                string name = existingName.ToString();

                if (name.Contains("Search"))
                { hasSearchButton = true; }

                if (name.Contains("Close"))
                {
                    object originalAction = MenuItemActionField.GetValue(item);

                    if (originalAction is Action originalCloseAction)
                    {
                        Action wrappedAction = () =>
                        {
                            SharedState.AllowNextClose = true;
                            originalCloseAction.Invoke();
                        };

                        MenuItemActionField.SetValue(item, wrappedAction);
                    }
                }
            }

            if (hasSearchButton)
            { return; }

            var ActionHandler = new SearchAction
            {
                owner = owner,
                container = container
            };

            // Create a brand new search bttn
            object searchMenuItem = Activator.CreateInstance(MenuItemClassType);

            MenuItemNameField.SetValue(
                searchMenuItem,
                LocalizedMethod.Invoke(
                    null,
                    new object[] { "Search", null }
                )
            );

            MenuItemActionField.SetValue(
                searchMenuItem,
                new Action(ActionHandler.StartSearch)
            );

            MenuItems.Insert(0, searchMenuItem);
        }

        public class SearchAction
        {
            public GamePlayerOwner owner;
            public LootableContainer container;

            public void StartSearch()
            {
                if (owner == null || container == null)
                    return;

                try
                {
                    owner.Player.SetCallbackForInteraction(
                        new Action<Action>((callback) =>
                        {
                            if (callback == null)
                            { Plugin.LogSource.LogWarning("Search callback was null"); }

                            float currentDistance = Vector3.Distance( owner.Player.Transform.position, container.transform.position );

                            ContainerSearchMethod.Invoke(
                                null,
                                new object[]
                                {
                                    owner,
                                    callback,
                                    container,
                                    currentDistance
                                }
                            );
                        })
                    );

                    // Skips the opening part of the search
                    owner.Player.TryInteractionCallback(container);
                }
                catch (Exception ex)
                { Plugin.LogSource.LogError(ex); }
            }
        }
    }
}