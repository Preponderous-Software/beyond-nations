using System.Numerics;
using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnations.desktop.input {

    /**
    * Applies KeyBindings to a running Simulation each frame.
    *
    * This is the replacement for the Input.GetKey/Input.GetKeyDown calls that
    * used to live in WorldScreen.readInput() (Assets/Scripts/screens --
    * that file is dead Unity code and out of scope here, see #221/#223, but
    * its binding table and behaviour are what this reproduces). Movement and
    * sprint are held-triggered; everything else -- interact, nation actions,
    * settlement actions, toggles and the debug commands -- is
    * pressed-this-frame, matching the old Input.GetKey vs Input.GetKeyDown
    * split exactly.
    *
    * Debug-gated commands (F2-F6) mirror WorldScreen's behaviour: they no-op
    * with a status message until F1 has been pressed at least once.
    */
    public class PlayerInputController {
        private const float DegreesToRadians = System.MathF.PI / 180f;

        private readonly MouseLook mouseLook = new MouseLook();
        private bool debugMode;
        private readonly ChronicleView chronicleView = new ChronicleView();
        private readonly BuildKey buildKey = new BuildKey();
        private readonly LeaveNationKey leaveNationKey = new LeaveNationKey();
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        public MouseLook getMouseLook() {
            return mouseLook;
        }

        /** Which chronicle view the HUD shows; KeyBindings.ToggleChronicle cycles it. */
        public ChronicleView getChronicleView() {
            return chronicleView;
        }

        public bool isDebugMode() {
            return debugMode;
        }

        /**
        * Lets --debug-mode open with the overlay already on. F1 toggles the
        * same field, so there is one answer to whether debug mode is on rather
        * than one per component.
        */
        public void setDebugMode(bool debugMode) {
            this.debugMode = debugMode;
        }

        public void update(Simulation simulation, InputService inputService) {
            Player player = simulation.getPlayer();

            // A right-button drag steers: sideways motion turns the player, so
            // W walks wherever the drag points, and up/down tilts the camera.
            // The camera stays behind the player rather than orbiting it.
            Vector2 mouseDelta = inputService.getMouseDelta();
            player.turn(-mouseDelta.X * MouseLook.DefaultSensitivityDegreesPerPixel * DegreesToRadians);
            mouseLook.apply(new Vector2(0f, mouseDelta.Y));

            applyMovement(player, inputService);
            applyActions(simulation, player, inputService);
        }

        private void applyMovement(Player player, InputService inputService) {
            // Unity's world is left-handed and this renderer's is right-handed,
            // so a rising yaw now turns the view to the left. Left therefore
            // raises yaw and right lowers it, the reverse of Unity's
            // Input.GetAxis("Horizontal").
            float horizontal = 0f;
            float vertical = 0f;
            if (inputService.isHeld(KeyBindings.MoveLeft) || inputService.isHeld(KeyBindings.MoveLeftAlt)) {
                horizontal += 1f;
            }
            if (inputService.isHeld(KeyBindings.MoveRight) || inputService.isHeld(KeyBindings.MoveRightAlt)) {
                horizontal -= 1f;
            }
            if (inputService.isHeld(KeyBindings.MoveForward) || inputService.isHeld(KeyBindings.MoveForwardAlt)) {
                vertical += 1f;
            }
            if (inputService.isHeld(KeyBindings.MoveBackward) || inputService.isHeld(KeyBindings.MoveBackwardAlt)) {
                vertical -= 1f;
            }
            // The browser's on-screen stick (touch screens) is used when no
            // movement key is held: up walks, down backs off, and sideways
            // turns, as A and D do, by as much as the stick is pushed.
            Vector2 stick = inputService.getMoveAxis();
            if (horizontal == 0f && vertical == 0f && stick != Vector2.Zero) {
                horizontal = stick.X == 0f ? 0f : -System.Math.Clamp(stick.X, -1f, 1f);
                vertical = System.Math.Clamp(stick.Y, -1f, 1f);
            }
            player.setMovementInput(horizontal, vertical);
            player.setSprinting(inputService.isHeld(KeyBindings.Sprint));

            if (inputService.wasPressedThisFrame(KeyBindings.Jump)) {
                player.requestJump();
            }
        }

        private void applyActions(Simulation simulation, Player player, InputService inputService) {
            // a "Press L/K again" prompt whose window has lapsed is taken back
            leaveNationKey.update(player, clock.Elapsed.TotalSeconds);
            buildKey.update(player, clock.Elapsed.TotalSeconds);
            if (inputService.wasPressedThisFrame(KeyBindings.Interact)) {
                new InteractCommand(simulation.getEnvironment(), simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getRandom(), simulation.getTickCounter())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.GiftToPawn)) {
                Pawn pawn = PawnRelations.findPawnInRange(player, simulation.getEnvironment());
                if (pawn == null) {
                    player.getStatus().update("There is no one within reach to give a gift to.");
                }
                else {
                    new GiftPawnCommand(simulation.getEventProducer()).execute(player, pawn);
                }
            }
            if (inputService.wasPressedThisFrame(KeyBindings.InviteToNation)) {
                Pawn pawn = PawnRelations.findPawnInRange(player, simulation.getEnvironment());
                if (pawn == null) {
                    player.getStatus().update("There is no one within reach to invite.");
                }
                else {
                    new InvitePawnToNationCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository())
                        .execute(player, pawn);
                }
            }
            if (inputService.wasPressedThisFrame(KeyBindings.CreateNewNation)) {
                new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.JoinNation)) {
                new NationJoinCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.LeaveNation)) {
                leaveNationKey.press(simulation, clock.Elapsed.TotalSeconds);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.FoundSettlement)) {
                new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.PlantSapling)) {
                new PlantSaplingCommand(simulation.getEntityRepository(), simulation.getRandom(), simulation.getTickCounter())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.TeleportToHomeSettlement)) {
                new TeleportHomeCommand(simulation.getEntityRepository(), simulation.getRandom())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.BuildStall)) {
                new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                    .execute(player);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.BuildBuilding)) {
                // twice to build: the first press names what and what it costs
                buildKey.press(simulation, clock.Elapsed.TotalSeconds);
            }
            if (inputService.wasPressedThisFrame(KeyBindings.ToggleChronicle)) {
                player.getStatus().update(ChronicleView.describe(chronicleView.cycle()));
            }
            if (inputService.wasPressedThisFrame(KeyBindings.ToggleAutoWalk)) {
                player.toggleAutoWalk();
            }
            if (inputService.wasPressedThisFrame(KeyBindings.IncreaseRenderDistance)) {
                player.increaseRenderDistance();
            }
            if (inputService.wasPressedThisFrame(KeyBindings.DecreaseRenderDistance)) {
                player.decreaseRenderDistance();
            }
            if (inputService.wasPressedThisFrame(KeyBindings.ToggleDebugMode)) {
                debugMode = !debugMode;
                player.getStatus().update(debugMode ? "Debug mode enabled." : "Debug mode disabled.");
            }
            if (inputService.wasPressedThisFrame(KeyBindings.GenerateNearbyLand)) {
                runIfDebugMode(player, "generate nearby land", () =>
                    new GenerateLandCommand(simulation.getEnvironment(), simulation.getWorldGenerator(), simulation.getGameConfig(), simulation.getRandom())
                        .execute(player));
            }
            if (inputService.wasPressedThisFrame(KeyBindings.SpawnNewPawn)) {
                runIfDebugMode(player, "spawn a pawn", () =>
                    new SpawnPawnCommand(simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getRandom(), simulation.getPawnNameGenerator())
                        .execute(player));
            }
            if (inputService.wasPressedThisFrame(KeyBindings.SpawnMoney)) {
                runIfDebugMode(player, "spawn money", () => new SpawnMoneyCommand().execute(player));
            }
            if (inputService.wasPressedThisFrame(KeyBindings.SpawnWood)) {
                runIfDebugMode(player, "spawn wood", () => new SpawnWoodCommand().execute(player));
            }
            if (inputService.wasPressedThisFrame(KeyBindings.TeleportAllToPlayer)) {
                runIfDebugMode(player, "teleport all pawns to player", () =>
                    new TeleportAllPawnsCommand(simulation.getEntityRepository(), simulation.getRandom())
                        .execute(player));
            }
            // KeyBindings.TakeScreenshot is deliberately absent from this
            // table: capture needs a framebuffer to read from, so it is
            // handled by the host in Game.readInput() and reports its own
            // status from there (#246). Handling it here as well would have
            // the world screen fire twice on one press.
            //
            // KeyBindings.ToggleInventory is absent for a similar reason: it
            // changes screen rather than acting on the simulation, and it has
            // to work on the inventory screen, where this controller does not
            // run. Game.readInput() hands it to ScreenState (#181).
        }

        private void runIfDebugMode(Player player, string actionDescription, System.Action action) {
            if (!debugMode) {
                player.getStatus().update("Debug mode must be enabled to " + actionDescription + ". Press F1 to enable debug mode.");
                return;
            }
            action();
        }
    }
}
