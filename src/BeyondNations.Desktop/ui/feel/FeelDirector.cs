using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.ui.boxes;

namespace beyondnations.desktop.ui.feel {

    /**
    * How the game feels: sound, visual feedback and the first-five-minutes
    * tips, for both hosts (Game and WebGame) and both UI hosts.
    *
    * Each frame the host calls frame() once, decorate() on the captured
    * snapshot, adds getCameraShake() to where the camera follows, hands
    * setView() the camera's matrices, and the UI host calls drawOverlay()
    * inside its ImGui frame. FeelObserver says what happened; this turns it
    * into cues for the ISoundService, effects in JuiceEffects, and the tip
    * card from OnboardingHints.
    *
    * Presentation only: nothing here writes to the simulation or draws from
    * its random source, so a world steps exactly the same with all of it
    * switched off. The history replay check relies on that.
    */
    public class FeelDirector {
        /** Past this, a building going up somewhere else is not heard. */
        public const float HearingDistance = 90f;
        /** Ambient loop level relative to the master volume. */
        public const float AmbientLevel = 0.45f;

        private static readonly Vector4 GainColour = new Vector4(0.75f, 1f, 0.6f, 1f);
        private static readonly Vector4 WoodColour = new Vector4(1f, 0.82f, 0.55f, 1f);
        private static readonly Vector4 StoneColour = new Vector4(0.86f, 0.88f, 0.92f, 1f);
        private static readonly Vector4 CoinColour = new Vector4(1f, 0.86f, 0.3f, 1f);
        private static readonly Vector4 LossColour = new Vector4(1f, 0.6f, 0.5f, 1f);
        private static readonly Vector4 WarningColour = new Vector4(1f, 0.62f, 0.25f, 1f);
        private static readonly Vector4 HintHeadingColour = new Vector4(1f, 0.84f, 0.35f, 1f);

        private readonly ISoundService sound;
        private readonly FeelSettings settings;
        private readonly IFeelSettingsStore store;
        private readonly bool touch;
        private readonly FeelObserver observer = new FeelObserver();
        private readonly JuiceEffects juice = new JuiceEffects();
        private readonly OnboardingHints hints;
        private readonly List<FeelEvent> events = new List<FeelEvent>();

        private Simulation simulation;
        private Matrix4x4 viewProjection = Matrix4x4.Identity;
        private bool hasView;
        private ScreenType lastScreen = (ScreenType) (-1);
        private bool clickedThisFrame;
        private AmbientKind ambient = AmbientKind.None;
        private float ambientVolume = -1f;
        private bool demo;
        private int demoFrames;

        public FeelDirector(ISoundService sound, IFeelSettingsStore store, bool touch) {
            this.sound = sound ?? new NullSoundService();
            this.store = store;
            this.touch = touch;
            this.settings = FeelSettings.load(store);
            this.hints = new OnboardingHints(settings);
            this.settings.setSoundAvailable(!(this.sound is NullSoundService));
            Log.info("sound: " + this.sound.getName() + (this.sound is NullSoundService ? " (this build has no audio backend)" : ""));
        }

        public FeelSettings getSettings() { return settings; }
        public OnboardingHints getHints() { return hints; }
        public JuiceEffects getJuice() { return juice; }
        public ISoundService getSound() { return sound; }

        /**
        * Whether tips are worded and sized for the on-screen controls: the
        * browser's touch layout when the host reports one (TouchLayout,
        * which follows the player's setting), else what the host said at start.
        */
        public bool isTouch() { return TouchLayout.isAvailable() ? TouchLayout.isActive() : touch; }

        /** --stage-feel replays the effects on every frame that is a multiple of this. */
        public const int DemoEveryFrames = 20;

        /** --stage-feel: replay the effects every DemoEveryFrames frames (stageDemo). */
        public void setDemo(bool on) { demo = on; }

        /** A world was built (newWorld) or loaded; tips run only for a new one. */
        public void beginWorld(Simulation world, bool newWorld) {
            simulation = world;
            observer.attach(world);
            juice.clear();
            hints.begin(newWorld);
        }

        /**
        * Once a frame on every screen, after the simulation has stepped and
        * before the world is drawn. deltaSeconds is real frame time.
        */
        public void frame(Simulation world, ScreenType screen, double deltaSeconds) {
            float dt = (float) Math.Clamp(deltaSeconds, 0.0, 0.25);
            clickedThisFrame = false;
            if (world != simulation) {
                // a world the host did not announce (should not happen): watch it, without tips
                beginWorld(world, false);
            }

            if (lastScreen != (ScreenType) (-1) && screen != lastScreen) {
                click();
            }
            lastScreen = screen;

            bool inWorld = world != null && (screen == ScreenType.WORLD);
            if (world != null) {
                events.Clear();
                observer.observe(events);
                // Gains and losses on other screens (crafting, inventory) still
                // sound, but effects that need the world in view wait for it.
                for (int i = 0; i < events.Count; i++) {
                    handle(events[i], world.getPlayer().getPosition());
                }
            }
            // Counted in frames, not seconds, so a screenshot taken a few
            // frames after a multiple of DemoEveryFrames always catches one.
            if (demo && inWorld && ++demoFrames % DemoEveryFrames == 0) {
                stageDemo(world);
            }
            juice.update(dt);
            if (inWorld) {
                hints.update(world, dt);
            }

            updateAmbient(world, screen);

            if (settings.consumeChanged()) {
                settings.save(store);
            }
        }

        private void updateAmbient(Simulation world, ScreenType screen) {
            AmbientKind wanted = AmbientKind.None;
            bool worldShown = world != null && screen != ScreenType.TITLE && screen != ScreenType.MAIN_MENU
                && screen != ScreenType.CONFIG && screen != ScreenType.LOAD;
            if (worldShown) {
                wanted = world.getTimeOfDay().isNight() ? AmbientKind.Night : AmbientKind.Day;
            }
            float volume = settings.getEffectiveVolume() * AmbientLevel * (screen == ScreenType.WORLD ? 1f : 0.5f);
            if (wanted != ambient || Math.Abs(volume - ambientVolume) > 0.001f) {
                ambient = wanted;
                ambientVolume = volume;
                sound.setAmbient(wanted, volume);
            }
        }

        private void play(SoundCue cue, float volume) {
            float gain = settings.getEffectiveVolume() * volume;
            if (gain > 0.001f) {
                sound.play(cue, gain);
            }
        }

        /** Quieter with distance from the player; silent past HearingDistance. */
        public static float attenuate(Vector3 source, Vector3 listener) {
            float distance = Vector3.Distance(source, listener);
            if (distance >= HearingDistance) {
                return 0f;
            }
            return Math.Clamp(1f - distance / HearingDistance, 0.2f, 1f);
        }

        private void handle(FeelEvent e, Vector3 listener) {
            switch (e.kind) {
                case FeelEventKind.Chop:
                    play(SoundCue.Chop, 0.9f);
                    juice.burst(e.position, JuiceEffects.WoodChip, 12, 5f, 0.28f);
                    juice.burst(e.position + new Vector3(0f, 2.5f, 0f), JuiceEffects.LeafBit, 8, 3.5f, 0.3f);
                    juice.shake(0.05f);
                    break;
                case FeelEventKind.Mine:
                    play(SoundCue.Mine, 0.9f);
                    juice.burst(e.position, JuiceEffects.StoneChip, 14, 5.5f, 0.24f);
                    juice.burst(e.position, JuiceEffects.Spark, 4, 6f, 0.12f);
                    juice.shake(0.07f);
                    break;
                case FeelEventKind.Hunt:
                    play(SoundCue.CombatHit, 0.9f);
                    juice.burst(e.position, JuiceEffects.Feather, 14, 4f, 0.2f);
                    juice.shake(0.12f);
                    break;
                case FeelEventKind.Pickup:
                    play(SoundCue.Pickup, 0.8f);
                    break;
                case FeelEventKind.Craft:
                    play(SoundCue.Craft, 0.9f);
                    juice.burst(e.position, JuiceEffects.Spark, 10, 4f, 0.14f);
                    break;
                case FeelEventKind.Coin:
                    play(SoundCue.Coin, 0.8f);
                    break;
                case FeelEventKind.BuildComplete:
                    float heard = attenuate(e.position, listener);
                    if (heard > 0f) {
                        play(SoundCue.BuildComplete, 0.9f * heard);
                    }
                    if (e.rises) {
                        juice.rise(e.position);
                    }
                    juice.burst(e.position, JuiceEffects.Dust, 10, 3f, 0.35f);
                    break;
                case FeelEventKind.HungerWarning:
                    play(SoundCue.HungerWarning, 0.9f);
                    juice.floatText(listener, "Hungry!", WarningColour);
                    break;
                case FeelEventKind.ObjectiveComplete:
                    play(SoundCue.ObjectiveComplete, 1f);
                    juice.flash();
                    break;
                case FeelEventKind.Raided:
                    play(SoundCue.CombatHit, 1f);
                    juice.shake(0.25f);
                    juice.floatText(listener, "Raided!", LossColour);
                    break;
                case FeelEventKind.Gain:
                    juice.floatText(listener, describeGain(e.item, e.amount), gainColour(e.item, e.amount));
                    break;
            }
        }

        /**
        * Every effect at once, for screenshots: chips from the tree and rock
        * nearest the player, floating gains over the player, the nearest
        * settlement's buildings rising again, and the objective flash. It
        * reads the world and changes nothing in it.
        */
        public void stageDemo(Simulation world) {
            Player player = world.getPlayer();
            Vector3 at = player.getPosition();
            Entity tree = world.getEnvironment().getNearestTree(at);
            Entity rock = world.getEnvironment().getNearestRock(at);
            Vector3 ahead = at + player.getForward() * 6f;
            juice.burst(tree != null && Vector3.Distance(tree.getPosition(), at) < 40f ? tree.getPosition() : ahead, JuiceEffects.WoodChip, 14, 5f, 0.3f);
            juice.burst(rock != null && Vector3.Distance(rock.getPosition(), at) < 40f ? rock.getPosition() : ahead, JuiceEffects.StoneChip, 14, 5.5f, 0.26f);
            juice.floatText(at, describeGain(ItemType.WOOD, 5), WoodColour);
            juice.floatText(at, describeGain(ItemType.STONE, 3), StoneColour);
            juice.floatText(at, describeGain(ItemType.COIN, 12), CoinColour);
            Settlement settlement = (Settlement) world.getEnvironment().getNearestEntityOfType(at, EntityType.SETTLEMENT);
            if (settlement != null && Vector3.Distance(settlement.getPosition(), at) < 80f) {
                foreach (Building building in settlement.getBuildings()) {
                    Vector3 slot = SettlementLayout.getBuildingSlotOffset(building.getSlot());
                    juice.rise(new Vector3(settlement.getPosition().X + slot.X, SettlementLayout.GroundTopY, settlement.getPosition().Z + slot.Z));
                }
            }
            juice.flash();
            play(SoundCue.BuildComplete, 0.8f);
        }

        /** "+5 stone", "+1 axe", "-20 coins". */
        public static string describeGain(ItemType item, int amount) {
            string sign = amount > 0 ? "+" : "-";
            return sign + ItemNames.describeQuantity(item, Math.Abs(amount));
        }

        private static Vector4 gainColour(ItemType item, int amount) {
            if (amount < 0) {
                return LossColour;
            }
            switch (item) {
                case ItemType.WOOD: return WoodColour;
                case ItemType.STONE: return StoneColour;
                case ItemType.COIN: return CoinColour;
                default: return GainColour;
            }
        }

        // --- the world pass ---

        public void decorate(WorldSnapshot snapshot) {
            juice.decorate(snapshot);
        }

        public Vector3 getCameraShake() {
            return juice.getShakeOffset();
        }

        public void setView(Matrix4x4 view, Matrix4x4 projection) {
            viewProjection = view * projection;
            hasView = true;
        }

        // --- the UI pass ---

        /** A button was pressed somewhere in the UI this frame. */
        public void click() {
            if (clickedThisFrame) {
                return;
            }
            clickedThisFrame = true;
            play(SoundCue.UiClick, 0.6f);
        }

        /**
        * Inside the UI host's ImGui frame, after the screen has drawn: the
        * objective flash and floating text under the HUD's panels, and the
        * tip card over them. Also listens for clicks on any button.
        */
        public void drawOverlay(float width, float height, ScreenType screen, Simulation world) {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsAnyItemHovered()) {
                click();
            }
            if (world == null || screen != ScreenType.WORLD) {
                return;
            }
            ImDrawListPtr background = ImGui.GetBackgroundDrawList();
            float flash = juice.getFlashStrength();
            if (flash > 0f) {
                background.AddRectFilled(Vector2.Zero, new Vector2(width, height),
                    ImGui.GetColorU32(new Vector4(1f, 0.88f, 0.45f, 0.32f * flash * flash)));
            }
            if (hasView) {
                drawFloatingTexts(background, width, height);
            }
            drawHintCard(width, height, world);
        }

        /** Where a world point lands on screen, or false when it is behind the camera. */
        public static bool project(Matrix4x4 viewProjection, Vector3 point, float width, float height, out Vector2 screen) {
            Vector4 clip = Vector4.Transform(new Vector4(point, 1f), viewProjection);
            if (clip.W <= 0.0001f) {
                screen = Vector2.Zero;
                return false;
            }
            float x = clip.X / clip.W;
            float y = clip.Y / clip.W;
            screen = new Vector2((x * 0.5f + 0.5f) * width, (1f - (y * 0.5f + 0.5f)) * height);
            return x > -1.2f && x < 1.2f && y > -1.2f && y < 1.2f;
        }

        private void drawFloatingTexts(ImDrawListPtr drawList, float width, float height) {
            IReadOnlyList<JuiceEffects.FloatingText> texts = juice.getFloatingTexts();
            if (texts.Count == 0) {
                return;
            }
            ImFontPtr font = ImGui.GetFont();
            float size = ImGui.GetFontSize() * 1.3f;
            float ratio = size / ImGui.GetFontSize();
            for (int i = 0; i < texts.Count; i++) {
                JuiceEffects.FloatingText text = texts[i];
                Vector3 at = text.position + new Vector3(0f, JuiceEffects.textRise(text.age), 0f);
                if (!project(viewProjection, at, width, height, out Vector2 screen)) {
                    continue;
                }
                float alpha = JuiceEffects.textAlpha(text.age);
                Vector2 measured = ImGui.CalcTextSize(text.text) * ratio;
                Vector2 topLeft = screen - measured * 0.5f;
                drawList.AddText(font, size, topLeft + new Vector2(1.5f, 1.5f), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.7f * alpha)), text.text);
                Vector4 colour = text.colour;
                colour.W *= alpha;
                drawList.AddText(font, size, topLeft, ImGui.GetColorU32(colour), text.text);
            }
        }

        private const ImGuiWindowFlags HintFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav;

        /** The tip card's width for a screen: comfortable to read, never wider than the screen. */
        public static float hintCardWidth(float screenWidth) {
            return Math.Min(screenWidth - UiScale.px(20f), UiScale.px(380f));
        }

        /**
        * Below the player, where the third-person view shows only ground and
        * the floating "+5 wood" over the player's head stays clear. On a
        * phone that band belongs to the on-screen controls, so the card
        * sits in the sky instead.
        */
        public static float hintCardTop(float screenHeight, bool touch) {
            return screenHeight * (touch ? 0.24f : 0.6f);
        }

        private void drawHintCard(float width, float height, Simulation world) {
            OnboardingHints.Step? step = hints.getVisible();
            // The objective toast says something more important; the tip waits.
            if (step == null || ObjectiveTrackerBox.isToastShowing(world)) {
                return;
            }
            float cardWidth = hintCardWidth(width);
            ImGui.SetNextWindowPos(new Vector2(width / 2f, hintCardTop(height, isTouch())), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowSize(new Vector2(cardWidth, 0f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.82f);
            if (ImGui.Begin("##feel-hint", HintFlags)) {
                Overlay.textColoured(HintHeadingColour,
                    "Tip " + hints.getNumber() + " of " + OnboardingHints.Steps.Length + ": " + OnboardingHints.title(step.Value));
                ImGui.PushTextWrapPos(0f);
                ImGui.TextUnformatted(OnboardingHints.text(step.Value, isTouch()));
                ImGui.PopTextWrapPos();
                // touch-sized buttons on a phone, ordinary ones with a mouse
                float buttonHeight = isTouch() ? UiScale.px(34f) : 0f;
                float buttonWidth = UiScale.px(isTouch() ? 96f : 80f);
                if (ImGui.Button("Got it", new Vector2(buttonWidth, buttonHeight))) {
                    hints.dismiss();
                }
                ImGui.SameLine();
                if (ImGui.Button("No tips", new Vector2(buttonWidth, buttonHeight))) {
                    hints.skipAll();
                }
            }
            ImGui.End();
        }

        /** For the browser page's status (and its smoke test): the audio state and the tip showing. */
        public string describeHint() {
            OnboardingHints.Step? step = hints.getVisible();
            return step == null ? "" : OnboardingHints.id(step.Value);
        }
    }
}
