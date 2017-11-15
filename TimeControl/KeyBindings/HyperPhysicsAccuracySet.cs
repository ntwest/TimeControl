using System;

namespace TimeControl.KeyBindings
{
    public class HyperPhysicsAccuracySet : TimeControlKeyBindingValue
    {
        private float v = 1f;

        private void UpdateDescription()
        {
            Description = String.Format( "Set Hyper-Warp Accuracy to {0}", v );
        }

        public HyperPhysicsAccuracySet()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperPhysicsAccuracySet;
            SetDescription = "Hyper-Warp Set Accuracy To: ";
            UpdateDescription();
        }

        public override float VMax
        {
            get => HyperWarpController.PhysicsAccuracyMax;
        }

        public override float VMin
        {
            get => HyperWarpController.PhysicsAccuracyMin;
        }

        public override float V
        {
            get => v;
            set
            {
                if (value >= VMax)
                {
                    v = VMax;
                }
                else if (value <= VMin)
                {
                    v = VMin;
                }
                else
                {
                    v = (float)Math.Round( value, 2 );
                }

                UpdateDescription();
            }
        }

        public override void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.PhysicsAccuracy = v;
            }
        }
    }
}
