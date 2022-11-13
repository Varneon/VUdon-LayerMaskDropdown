using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharpEditor;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRC.Udon;
using static UnityEngine.EventSystems.EventTrigger;

namespace Varneon.VUdon.LayerMaskDropdown.Editor
{
    internal static class LayerMaskDropdownPostProcessor
    {
        [PostProcessScene(-1)] // Ensure that all of the dropdowns get processed before U# saves the C# component data into UdonBehaviours
        private static void InitializeLayerMaskDropdowns()
        {
            // Find all LayerMaskDropdowns and setup all of them
            foreach(LayerMaskDropdown dropdown in UnityEngine.Object.FindObjectsOfType<LayerMaskDropdown>())
            {
                SetupLayerMaskDropdown(dropdown);
            }
        }

        /// <summary>
        /// Sets up a LayerMaskDropdown to match the project's current settings and links the required components 
        /// </summary>
        /// <param name="layerMaskDropdown"></param>
        private static void SetupLayerMaskDropdown(LayerMaskDropdown layerMaskDropdown)
        {
            List<int> layerIndices = new List<int>();

            List<string> optionNames = new List<string>() { "Nothing", "Everything" };

            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);

                bool isDefined = !string.IsNullOrEmpty(name);

                if (isDefined)
                {
                    optionNames.Add(string.Format("{0}: {1}", i, name));

                    layerIndices.Add(i);
                }
            }

            Dropdown dropdown = layerMaskDropdown.GetComponent<Dropdown>();

            FieldInfo layerCountField = typeof(LayerMaskDropdown).GetField("projectLayerCount", BindingFlags.Instance | BindingFlags.NonPublic);

            FieldInfo layersField = typeof(LayerMaskDropdown).GetField("layerIndices", BindingFlags.Instance | BindingFlags.NonPublic);

            EventTrigger trigger = layerMaskDropdown.GetComponent<EventTrigger>();

            layerCountField.SetValue(layerMaskDropdown, optionNames.Count - 2);

            layersField.SetValue(layerMaskDropdown, layerIndices.ToArray());

            dropdown.options = optionNames.Select(l => new Dropdown.OptionData(l)).ToList();

            UdonBehaviour ub = UdonSharpEditorUtility.GetBackingUdonBehaviour(layerMaskDropdown);

            MethodInfo sendCustomEventInfo = UnityEventBase.GetValidMethodInfo(ub, nameof(UdonBehaviour.SendCustomEvent), new[] { typeof(string) });

            UnityAction<string> sendCustomEventDelegate = Delegate.CreateDelegate(typeof(UnityAction<string>), ub, sendCustomEventInfo, false) as UnityAction<string>;

            UnityEventTools.AddStringPersistentListener(dropdown.onValueChanged, sendCustomEventDelegate, "OnValueChanged");

            Entry dropdownClickEntry = new Entry
            {
                eventID = EventTriggerType.PointerClick,
                callback = new TriggerEvent()
            };
            UnityEventTools.AddStringPersistentListener(dropdownClickEntry.callback, sendCustomEventDelegate, "OnClick");

            Toggle templateItem = dropdown.template.GetComponentInChildren<Toggle>();

            EventTrigger templateItemTrigger = templateItem.GetComponent<EventTrigger>() ?? templateItem.gameObject.AddComponent<EventTrigger>();

            Entry itemClickEntry = new Entry
            {
                eventID = EventTriggerType.PointerClick,
                callback = new TriggerEvent()
            };
            UnityEventTools.AddStringPersistentListener(itemClickEntry.callback, sendCustomEventDelegate, "OnItemClick");

            trigger.triggers.Add(dropdownClickEntry);
            templateItemTrigger.triggers.Add(itemClickEntry);
        }
    }
}
