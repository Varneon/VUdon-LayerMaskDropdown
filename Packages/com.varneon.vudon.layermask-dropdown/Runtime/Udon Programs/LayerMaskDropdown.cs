using UdonSharp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRC.Udon;

namespace Varneon.VUdon.LayerMaskDropdown
{
    [RequireComponent(typeof(Dropdown))]
    [RequireComponent(typeof(EventTrigger))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LayerMaskDropdown : UdonSharpBehaviour
    {
        public LayerMask Value => mask;

        /// <summary>
        /// Current LayerMask
        /// </summary>
        [SerializeField]
        private LayerMask mask;

        /// <summary>
        /// Target UdonBehaviour for applying the LayerMask value
        /// </summary>
        [SerializeField]
        private UdonBehaviour target;

        /// <summary>
        /// Name of the target UdonBehaviour's LayerMask variable
        /// </summary>
        [SerializeField]
        private string variable;

        /// <summary>
        /// Name of the target UdonBehaviour's method
        /// </summary>
        [SerializeField]
        private string method;

        /// <summary>
        /// Number of layers defined in this project
        /// </summary>
        [SerializeField, HideInInspector]
        private int projectLayerCount;

        /// <summary>
        /// Indices of all of the defined layers in the project
        /// </summary>
        [SerializeField, HideInInspector]
        private int[] layerIndices;

        private Dropdown dropdown;

        private int maskInt;

        private Toggle[] cullingMaskToggles;

        private Text dropdownValueLabel;

        private bool isOpen;

        private bool selectionQueued;

        private void Start()
        {
            dropdown = GetComponent<Dropdown>();

            dropdownValueLabel = dropdown.captionText;

            maskInt = mask;

            SetTargetVariable();

            CheckMixedLayers();
        }

        private void SetTargetVariable()
        {
            if(target != null)
            {
                if (!string.IsNullOrWhiteSpace(variable))
                {
                    target.SetProgramVariable(variable, mask);
                }

                if (!string.IsNullOrWhiteSpace(method))
                {
                    target.SendCustomEvent(method);
                }
            }
        }

        private void OpenCullingMaskMenu()
        {
            isOpen = true;

            cullingMaskToggles = dropdown.GetComponentsInChildren<Toggle>(true);

            for (int i = 0; i < projectLayerCount; i++)
            {
                cullingMaskToggles[i + 4].SetIsOnWithoutNotify((maskInt & (1 << layerIndices[i])) != 0);
            }
        }

        public void OnClick()
        {
            OpenCullingMaskMenu();
        }

        public void OnItemClick()
        {
            selectionQueued = true;

            SendCustomEventDelayedFrames(nameof(PostProcessItemClick), 0);
        }

        public void PostProcessItemClick()
        {
            if (!selectionQueued || !isOpen) { return; }

            UpdateCullingMask();
        }

        private void UpdateCullingMask()
        {
            int selectedLayerOption = dropdown.value;

            switch (selectedLayerOption)
            {
                case 0: // Nothing
                    maskInt = 0;
                    break;
                case 1: // Everything
                    maskInt = -1;
                    break;
                default:
                    int mask = 1 << layerIndices[selectedLayerOption - 2]; // Create bitmask of the layer that was clicked

                    if (isOpen)
                    {
                        if ((maskInt & mask) != 0) // If same bit is set on both the culling mask and the selection mask
                        {
                            maskInt &= ~mask; // Reset the bit
                        }
                        else
                        {
                            maskInt |= mask; // Set the bit
                        }
                    }

                    CheckMixedLayers();
                    break;
            }

            mask = maskInt;

            SetTargetVariable();

            selectionQueued = false;

            isOpen = false;
        }

        private void CheckMixedLayers()
        {
            int layerCount = 0;

            int optionIndex = 0;

            for (int i = 0; i < projectLayerCount; i++)
            {
                if ((maskInt & (1 << layerIndices[i])) != 0)
                {
                    optionIndex = i;

                    layerCount += 1;
                }
            }

            if (layerCount == projectLayerCount)
            {
                maskInt = -1;

                dropdown.SetValueWithoutNotify(1);
            }
            else if (layerCount > 1) // If multiple layers are selected, set the preview label as "Mixed..."
            {
                dropdown.SetValueWithoutNotify(optionIndex + 2);

                dropdownValueLabel.text = "Mixed...";
            }
            else if (layerCount == 1) // If only one layer is selected, override the dropdown with the remaining layer
            {
                dropdown.SetValueWithoutNotify(optionIndex + 2);
            }
            else if (layerCount == 0) // If no layers have been selected, override the dropdown to Nothing / 0
            {
                dropdown.SetValueWithoutNotify(0);
            }
        }

        public void SetValueWithoutNotify(LayerMask value)
        {
            dropdown = GetComponent<Dropdown>();

            dropdownValueLabel = dropdown.captionText;

            mask = value;

            maskInt = mask;

            CheckMixedLayers();
        }
    }
}
