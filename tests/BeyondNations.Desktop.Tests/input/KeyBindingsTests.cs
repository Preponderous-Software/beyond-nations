using Silk.NET.Input;
using Xunit;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    /**
    * Pins every binding to the README control table
    * (https://github.com/Preponderous-Software/beyond-nations#controls) so a
    * future edit that drifts from it fails loudly here rather than silently
    * in play.
    */
    public class KeyBindingsTests {

        [Fact]
        public void movementBindings_matchReadme() {
            Assert.Equal(Key.W, KeyBindings.MoveForward);
            Assert.Equal(Key.Up, KeyBindings.MoveForwardAlt);
            Assert.Equal(Key.S, KeyBindings.MoveBackward);
            Assert.Equal(Key.Down, KeyBindings.MoveBackwardAlt);
            Assert.Equal(Key.A, KeyBindings.MoveLeft);
            Assert.Equal(Key.Left, KeyBindings.MoveLeftAlt);
            Assert.Equal(Key.D, KeyBindings.MoveRight);
            Assert.Equal(Key.Right, KeyBindings.MoveRightAlt);
        }

        [Fact]
        public void actionBindings_matchReadme() {
            Assert.Equal(Key.E, KeyBindings.Interact);
            Assert.Equal(Key.Space, KeyBindings.Jump);
            Assert.Equal(Key.ShiftLeft, KeyBindings.Sprint);
            Assert.Equal(Key.N, KeyBindings.CreateNewNation);
            Assert.Equal(Key.J, KeyBindings.JoinNation);
            Assert.Equal(Key.L, KeyBindings.LeaveNation);
            Assert.Equal(Key.F, KeyBindings.FoundSettlement);
            Assert.Equal(Key.P, KeyBindings.PlantSapling);
            Assert.Equal(Key.H, KeyBindings.TeleportToHomeSettlement);
            Assert.Equal(Key.B, KeyBindings.BuildStall);
            Assert.Equal(Key.I, KeyBindings.ToggleInventory);
            Assert.Equal(Key.Insert, KeyBindings.ToggleAutoWalk);
            Assert.Equal(Key.V, KeyBindings.ToggleCameraView);
            Assert.Equal(Key.PageUp, KeyBindings.IncreaseRenderDistance);
            Assert.Equal(Key.PageDown, KeyBindings.DecreaseRenderDistance);
            Assert.Equal(Key.Escape, KeyBindings.Pause);
        }

        [Fact]
        public void debugBindings_matchReadme() {
            Assert.Equal(Key.F1, KeyBindings.ToggleDebugMode);
            Assert.Equal(Key.F2, KeyBindings.GenerateNearbyLand);
            Assert.Equal(Key.F3, KeyBindings.SpawnNewPawn);
            Assert.Equal(Key.F4, KeyBindings.SpawnMoney);
            Assert.Equal(Key.F5, KeyBindings.SpawnWood);
            Assert.Equal(Key.F6, KeyBindings.TeleportAllToPlayer);
            Assert.Equal(Key.F12, KeyBindings.TakeScreenshot);
        }

        [Fact]
        public void noTwoBindings_shareAKey() {
            // Every binding above should be distinct; a collision would mean
            // two actions silently firing off the same key press. The alt
            // movement keys are intentionally excluded, since they are meant
            // to duplicate their primary.
            Key[] bindings = {
                KeyBindings.MoveForward, KeyBindings.MoveBackward, KeyBindings.MoveLeft, KeyBindings.MoveRight,
                KeyBindings.Interact, KeyBindings.Jump, KeyBindings.Sprint,
                KeyBindings.CreateNewNation, KeyBindings.JoinNation, KeyBindings.LeaveNation,
                KeyBindings.FoundSettlement, KeyBindings.PlantSapling, KeyBindings.TeleportToHomeSettlement,
                KeyBindings.BuildStall, KeyBindings.ToggleInventory, KeyBindings.ToggleAutoWalk,
                KeyBindings.ToggleCameraView,
                KeyBindings.IncreaseRenderDistance, KeyBindings.DecreaseRenderDistance, KeyBindings.Pause,
                KeyBindings.ToggleDebugMode, KeyBindings.GenerateNearbyLand, KeyBindings.SpawnNewPawn,
                KeyBindings.SpawnMoney, KeyBindings.SpawnWood, KeyBindings.TeleportAllToPlayer,
                KeyBindings.TakeScreenshot
            };

            System.Collections.Generic.HashSet<Key> seen = new System.Collections.Generic.HashSet<Key>();
            foreach (Key key in bindings) {
                Assert.True(seen.Add(key), "Key " + key + " is bound to more than one action.");
            }
        }
    }
}
