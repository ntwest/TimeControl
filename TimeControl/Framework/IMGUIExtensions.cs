using System;
using UnityEngine;

namespace TimeControl.Framework
{
    /// <summary>
    /// Collection of functions that create IMGUI objects with backing fields
    /// </summary>
    public static class IMGUIExtensions
    {
        /// <summary>
        /// Creates a text box + slider that both update the same backing field. Must be run as part of an IMGUI function
        /// </summary>
        /// <param name="comboLabel">label for this control</param>
        /// <param name="backingFieldFloat">Value of the backing field</param>
        /// <param name="sliderMin">Minimim value for the backing field</param>
        /// <param name="sliderMax">Maximum value for the backing field</param>
        /// <param name="updateBackingField">Action that is called when we want to update the backing field</param>
        /// <param name="modifyField">Function that is applied to the GUI input prior to updating the backing field</param>
        public static void floatTextBoxAndSliderCombo(string comboLabel, float backingFieldFloat, float sliderMin, float sliderMax, Action<float> updateBackingField, Func<float, float> modifyField = null)
        {
            string backingFieldStr = backingFieldFloat.ToString();
            float fieldFloat;
            string fieldStr;

            if (comboLabel != null && comboLabel != "")
                GUILayout.Label( comboLabel );

            GUILayout.BeginHorizontal();
            {
                // Text Box to enter values
                fieldStr = GUILayout.TextField( backingFieldStr, GUILayout.Width( 35 ) );
                if (fieldStr != backingFieldStr && float.TryParse( fieldStr, out fieldFloat ))
                {
                    fieldFloat = Mathf.Clamp( fieldFloat, sliderMin, sliderMax );
                    backingFieldStr = fieldStr;
                    if (modifyField != null)
                        fieldFloat = modifyField( fieldFloat );
                    updateBackingField( fieldFloat );
                }

                // Slider to enter values
                fieldFloat = GUILayout.HorizontalSlider( backingFieldFloat, sliderMin, sliderMax );
                if (modifyField != null)
                    fieldFloat = modifyField( fieldFloat );
                if (fieldFloat != backingFieldFloat)
                {
                    updateBackingField( fieldFloat );
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
