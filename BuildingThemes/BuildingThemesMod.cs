﻿using ICities;
using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace BuildingThemes
{
    public class BuildingThemesMod : LoadingExtensionBase, IUserMod
    {
        public string Name
        {
            get { return "Building Themes"; }
        }

        public string Description
        {
            get { return "Create building themes and apply them to map themes, cities and districts."; }
        }

        private UIButton tab;

        public override void OnLevelLoaded(LoadMode mode) 
        {
            // Is it an actual game ?
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame) return;

            // TODO load data (serialized for policies, xml for themes)

            // Hook into policies GUI
            ToolsModifierControl.policiesPanel.component.eventVisibilityChanged += OnPoliciesPanelVisibilityChanged;

            // Replace BuildingManager. Credits to Traffic++ developers ;)
            ReplaceBuildingManager();
        }

        public override void OnLevelUnloading()
        {
            // Remove the custom policy tab
            RemoveThemesTab();
        }

        private string GetCurrentEnvironment()
        {
            return Singleton<SimulationManager>.instance.m_metaData.m_environment;
        }

        private void ReplaceBuildingManager()
        {
            if (Singleton<BuildingManager>.instance as CustomBuildingManager != null) return;

            FieldInfo sInstance = typeof(ColossalFramework.Singleton<BuildingManager>).GetField("sInstance", BindingFlags.NonPublic | BindingFlags.Static);
            BuildingManager originalBuildingManager = ColossalFramework.Singleton<BuildingManager>.instance;
            CustomBuildingManager customBuildingManager = originalBuildingManager.gameObject.AddComponent<CustomBuildingManager>();
            customBuildingManager.SetOriginalValues(originalBuildingManager);

            // change the new instance in the singleton
            sInstance.SetValue(null, customBuildingManager);

            // change the manager in the SimulationManager
            FastList<ISimulationManager> managers = (FastList<ISimulationManager>)typeof(SimulationManager).GetField("m_managers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            managers.Remove(originalBuildingManager);
            managers.Add(customBuildingManager);

            // add to renderable managers
            IRenderableManager[] renderables;
            int count;
            RenderManager.GetManagers(out renderables, out count);
            if (renderables != null && count != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    BuildingManager temp = renderables[i] as BuildingManager;
                    if (temp != null && temp == originalBuildingManager)
                    {
                        renderables[i] = customBuildingManager;
                        break;
                    }
                }
            }
            else
            {
                RenderManager.RegisterRenderableManager(customBuildingManager);
            }

            // Destroy in 10 seconds to give time to all references to update to the new manager without crashing
            GameObject.Destroy(originalBuildingManager, 10f);

            Debug.Log("Building Themes: Building Manager successfully replaced.");
        }


        // GUI stuff

        private void OnPoliciesPanelVisibilityChanged(UIComponent component, bool visible)
        {
            // It is necessary to remove the custom tab when the panel is closed 
            // because the game logic is coupled to the GUI
            if (visible)
            {
                AddThemesTab();
            }
            else
            {
                RemoveThemesTab();
            }
        }

        private void AddThemesTab()
        {
            UITabstrip tabstrip = ToolsModifierControl.policiesPanel.Find("Tabstrip") as UITabstrip;
            tab = tabstrip.AddTab("Themes");
            tab.stringUserData = "CityPlanning";

            // recalculate the width of the tabs
            for (int i = 0; i < tabstrip.tabCount; i++)
            {
                tabstrip.tabs[i].width = tabstrip.width / ((float)tabstrip.tabCount - 1);
            }

            // TODO this is hacky. better store it in a field
            GameObject go = GameObject.Find("Tab 5 - Themes");
            if (go == null)
            {
                return;
            }

            // remove the default stuff if something is in there
            foreach (Transform child in go.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            UIPanel container = go.GetComponent<UIPanel>();

            container.autoLayout = true;
            container.autoLayoutDirection = LayoutDirection.Vertical;
            container.autoLayoutPadding.top = 5;

            // add some sample buttons

            AddThemePolicyButton(container, "Chicago 1890");
            AddThemePolicyButton(container, "New York 1940");
            AddThemePolicyButton(container, "Houston 1990");
            AddThemePolicyButton(container, "Euro-Contemporary");
            AddThemePolicyButton(container, "My first custom theme");
        }

        private void RemoveThemesTab() 
        {
            // TODO this is hacky. better store it in a field
            GameObject go = GameObject.Find("Tab 5 - Themes");
            if (go == null)
            {
                return;
            }
            GameObject.Destroy(go);

            UITabstrip tabstrip = ToolsModifierControl.policiesPanel.Find("Tabstrip") as UITabstrip;
            tabstrip.RemoveUIComponent(tab);
            GameObject.Destroy(tab.gameObject);
        }

        private void AddThemePolicyButton(UIPanel container, string name) 
        {
            
            UIPanel policyPanel = container.AddUIComponent<UIPanel>();
            policyPanel.name = name;
            policyPanel.backgroundSprite = "GenericPanel";
            policyPanel.size = new Vector2(364f, 44f);
            policyPanel.objectUserData = ToolsModifierControl.policiesPanel;
            policyPanel.stringUserData = "None";

            UIButton policyButton = policyPanel.AddUIComponent<UIButton>();
            policyButton.name = "PolicyButton";
            policyButton.text = name;
            policyButton.size = new Vector2(324f, 40f);
            policyButton.focusedBgSprite = "PolicyBarBackActive";
            policyButton.normalBgSprite = "PolicyBarBack";
            policyButton.relativePosition = new Vector3(2f, 2f, 0f);
            policyButton.textPadding.left = 50;
            policyButton.textColor = new Color32(0,0,0,255);
            policyButton.disabledTextColor = new Color32(0, 0, 0, 255);
            policyButton.hoveredTextColor = new Color32(0, 0, 0, 255);
            policyButton.pressedTextColor = new Color32(0, 0, 0, 255);
            policyButton.focusedTextColor = new Color32(0, 0, 0, 255);
            policyButton.disabledColor = new Color32(124, 124, 124, 255);
            policyButton.dropShadowColor = new Color32(103, 103, 103, 255);
            policyButton.dropShadowOffset = new Vector2(1f, 1f);
            policyButton.textHorizontalAlignment = UIHorizontalAlignment.Left;
            policyButton.useDropShadow = false;
            policyButton.textScale = 0.875f;

            UICheckBox policyCheckBox = policyButton.AddUIComponent<UICheckBox>();
            policyCheckBox.name = "Checkbox";
            policyCheckBox.size = new Vector2(363f, 44f);
            policyCheckBox.relativePosition = new Vector3(0f, -2f, 0f);
            policyCheckBox.clipChildren = true;

            UISprite sprite = policyCheckBox.AddUIComponent<UISprite>();
            sprite.name = "Unchecked";
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(16f, 16f);
            sprite.relativePosition = new Vector3(336.6984f,14,0f);

            policyCheckBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
            policyCheckBox.checkedBoxObject.name = "Checked";
            ((UISprite)policyCheckBox.checkedBoxObject).spriteName = "ToggleBaseFocused";
            policyCheckBox.checkedBoxObject.size = new Vector2(16f, 16f);
            policyCheckBox.checkedBoxObject.relativePosition = Vector3.zero;

            // TODO link the checkbox and the focus of the button (like PolicyContainer component does)
        }

    }

    public class Configuration 
    { 
        // TODO the xml configuration for the themes
    }
}