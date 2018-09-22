using System;
using UnityEngine;
using System.Collections.Generic;

namespace TimeControl
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

        public static void floatTextBoxSliderPlusMinus(string comboLabel, float fbacking, float sliderMin, float sliderMax, float increment, Action<float> updateBackingField, Func<float, float> modifyField = null, bool reverse = false)
        {
            string backingStr = fbacking.ToString();
            float fvalue = fbacking;
            string fStr;
            
            if (comboLabel != null && comboLabel != "")
            {
                GUILayout.Label( comboLabel );
            }

            GUILayout.BeginHorizontal();
            {
                // Text Box to enter values
                fStr = GUILayout.TextField( backingStr, GUILayout.Width( 35 ) );
                if (fStr != backingStr && float.TryParse( fStr, out fvalue ))
                {
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    backingStr = fStr;
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    updateBackingField( fvalue );
                }

                // Plus / Minus buttons
                if (GUILayout.Button( "+", GUILayout.Width( 20 ) ))
                {
                    fvalue += increment;
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    if (fvalue != fbacking)
                    {
                        updateBackingField( fvalue );
                    }
                }

                if (GUILayout.Button( "-", GUILayout.Width( 20 ) ))
                {
                    fvalue -= increment;
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    if (fvalue != fbacking)
                    {
                        updateBackingField( fvalue );
                    }
                }

                // Slider to enter values
                if (!reverse)
                {
                    fvalue = GUILayout.HorizontalSlider( fbacking, sliderMin, sliderMax );
                }
                else
                {
                    fvalue = GUILayout.HorizontalSlider( fbacking, sliderMax, sliderMin );
                }
                fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                if (modifyField != null)
                {
                    fvalue = modifyField( fvalue );
                }
                if (fvalue != fbacking)
                {
                    updateBackingField( fvalue );
                }
                
            }
            GUILayout.EndHorizontal();
        }

        public static void floatTextBoxSliderPlusMinusWithButtonList(string comboLabel, float fbacking, float sliderMin, float sliderMax, float increment, Action<float> updateBackingField, List<float> immediateButtons, Func<float, float> modifyField = null, bool reverse = false)
        {
            string backingStr = fbacking.MemoizedToString();
            float fvalue = fbacking;
            string fStr;

            if (comboLabel != null && comboLabel != "")
            {
                GUILayout.Label( comboLabel );
            }

            GUILayout.BeginHorizontal();
            {
                // Text Box to enter values
                fStr = GUILayout.TextField( backingStr, GUILayout.Width( 35 ) );
                if (fStr != backingStr && float.TryParse( fStr, out fvalue ))
                {
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    backingStr = fStr;
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    updateBackingField( fvalue );
                }

                // Plus / Minus buttons
                if (GUILayout.Button( "+", GUILayout.Width( 20 ) ))
                {
                    fvalue += increment;
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    if (fvalue != fbacking)
                    {
                        updateBackingField( fvalue );
                    }
                }

                if (GUILayout.Button( "-", GUILayout.Width( 20 ) ))
                {
                    fvalue -= increment;
                    fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                    if (modifyField != null)
                    {
                        fvalue = modifyField( fvalue );
                    }
                    if (fvalue != fbacking)
                    {
                        updateBackingField( fvalue );
                    }
                }

                // Slider to enter values
                if (!reverse)
                {
                    fvalue = GUILayout.HorizontalSlider( fbacking, sliderMin, sliderMax );
                }
                else
                {
                    fvalue = GUILayout.HorizontalSlider( fbacking, sliderMax, sliderMin );
                }
                fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                if (modifyField != null)
                {
                    fvalue = modifyField( fvalue );
                }
                if (fvalue != fbacking)
                {
                    updateBackingField( fvalue );
                }


                if (immediateButtons != null)
                {
                    foreach (var v in immediateButtons)
                    {
                        if (v >= sliderMin && v <= sliderMax)
                        {
                            float buttonWidth = (v > 99) ? 35 : ((v > 9) ? 30 : 25);
                            if (GUILayout.Button( v.ToString(), GUILayout.Width( buttonWidth ) ))
                            {
                                fvalue = v;
                                fvalue = Mathf.Clamp( fvalue, sliderMin, sliderMax );
                                if (modifyField != null)
                                {
                                    fvalue = modifyField( fvalue );
                                }
                                if (fvalue != fbacking)
                                {
                                    updateBackingField( fvalue );
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
