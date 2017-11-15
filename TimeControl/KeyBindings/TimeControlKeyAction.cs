namespace TimeControl.KeyBindings
{
    public enum TimeControlKeyAction
    {
        GUIToggle = 1,
        Realtime = 2,
        PauseToggle = 3,
        TimeStep = 4,
        
        HyperToggle = 5,
        HyperActivate = 6,
        HyperDeactivate = 7,
        HyperRateSetRate = 8,
        HyperRateSpeedUp = 9,
        HyperRateSlowDown = 10,
        HyperPhysicsAccuracySet = 11,
        HyperPhysicsAccuracyUp = 12,
        HyperPhysicsAccuracyDown = 13,

        SlowMoToggle = 14,
        SlowMoActivate = 15,
        SlowMoDeactivate = 16,
        SlowMoSetRate = 17,
        SlowMoSpeedUp = 18,
        SlowMoSlowDown = 19,
        
        WarpToNextKACAlarm = 20,
        WarpForNOrbits = 21,

        WarpToVesselOrbitLocation = 22,
        WarpForNTimeIncrements = 23
    }
}
