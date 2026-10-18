using Silk.NET.Input;

namespace beyondnations.desktop.input {

    /**
    * The binding table, moved from Assets/Scripts/config/KeyBindings.cs and
    * retargeted from UnityEngine.KeyCode to Silk.NET.Input.Key. This is host
    * input configuration, not simulation, so it lives here rather than in
    * BeyondNations.Core -- see #217.
    *
    * Every entry corresponds to a row in the README control table. The four
    * primary movement keys have arrow-key alternates, matching the README's
    * "W / Up Arrow" style rows and the old Input.GetAxis("Horizontal"/
    * "Vertical") behaviour, which accepted both.
    */
    public static class KeyBindings {
        public static readonly Key MoveForward = Key.W;
        public static readonly Key MoveForwardAlt = Key.Up;
        public static readonly Key MoveBackward = Key.S;
        public static readonly Key MoveBackwardAlt = Key.Down;
        public static readonly Key MoveLeft = Key.A;
        public static readonly Key MoveLeftAlt = Key.Left;
        public static readonly Key MoveRight = Key.D;
        public static readonly Key MoveRightAlt = Key.Right;
        public static readonly Key Interact = Key.E;
        public static readonly Key Jump = Key.Space;
        public static readonly Key Sprint = Key.ShiftLeft;
        public static readonly Key CreateNewNation = Key.N;
        public static readonly Key JoinNation = Key.J;
        public static readonly Key LeaveNation = Key.L;
        public static readonly Key FoundSettlement = Key.F;
        public static readonly Key PlantSapling = Key.P;
        public static readonly Key TeleportToHomeSettlement = Key.H;
        public static readonly Key BuildStall = Key.B;
        public static readonly Key ToggleInventory = Key.I;
        public static readonly Key ToggleAutoWalk = Key.Insert;
        public static readonly Key ToggleCameraView = Key.V;
        public static readonly Key IncreaseRenderDistance = Key.PageUp;
        public static readonly Key DecreaseRenderDistance = Key.PageDown;
        public static readonly Key Pause = Key.Escape;
        public static readonly Key ToggleDebugMode = Key.F1;
        public static readonly Key GenerateNearbyLand = Key.F2;
        public static readonly Key SpawnNewPawn = Key.F3;
        public static readonly Key SpawnMoney = Key.F4;
        public static readonly Key SpawnWood = Key.F5;
        public static readonly Key TeleportAllToPlayer = Key.F6;
        public static readonly Key TakeScreenshot = Key.F12;
    }
}
