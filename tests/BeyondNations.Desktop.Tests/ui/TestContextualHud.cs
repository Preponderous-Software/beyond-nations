using System.Collections.Generic;
using System.Linq;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui.hud;
using beyondnations.desktop.ui.progression;

namespace beyondnationstests.desktop.ui {

    /**
    * The contextual HUD shell's pure parts: the overlay queue (one at a
    * time, by priority, waiting for a calm moment), what the feed announces
    * (and that a load announces nothing), the NEXT line, when each piece is
    * relevant, the meters, the status line's expiry, and the unlock seam.
    */
    public class TestOverlayQueue {
        private static OverlayItem item(OverlayKind kind, string key, double seconds = 3) {
            return new OverlayItem(kind, key, key, "", seconds);
        }

        [Fact]
        public void oneAtATime_highestPriorityFirst_oldestFirstAmongEquals() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Toast, "toast-a"), 0);
            queue.enqueue(item(OverlayKind.Tip, "tip"), 0);
            queue.enqueue(item(OverlayKind.Unlock, "unlock-a"), 0);
            queue.enqueue(item(OverlayKind.Unlock, "unlock-b"), 0);
            queue.update(1, OverlayContext.Calm);
            Assert.Equal("unlock-a", queue.getCurrent().Key);
            Assert.Equal(3, queue.getQueuedCount());
            List<string> order = new List<string> { queue.getCurrent().Key };
            double now = 1;
            while (queue.getCurrent() != null) {
                queue.dismiss(now);
                now += OverlayQueue.CalmGapSeconds;
                queue.update(now, OverlayContext.Calm);
                if (queue.getCurrent() != null) {
                    order.Add(queue.getCurrent().Key);
                }
            }
            Assert.Equal(new[] { "unlock-a", "unlock-b", "tip", "toast-a" }, order);
        }

        [Fact]
        public void thePriorities_areEnd_Event_Unlock_Tip_Toast() {
            OverlayQueue queue = new OverlayQueue();
            foreach (OverlayKind kind in new[] { OverlayKind.Toast, OverlayKind.Tip, OverlayKind.Unlock, OverlayKind.Event, OverlayKind.End }) {
                queue.enqueue(item(kind, kind.ToString(), 0), 0);
            }
            List<OverlayKind> order = new List<OverlayKind>();
            double now = 0;
            for (int i = 0; i < 5; i++) {
                queue.update(now, OverlayContext.Calm);
                order.Add(queue.getCurrent().Kind);
                queue.dismiss(now);
                now += 1;
            }
            Assert.Equal(new[] { OverlayKind.End, OverlayKind.Event, OverlayKind.Unlock, OverlayKind.Tip, OverlayKind.Toast }, order);
        }

        [Fact]
        public void aCard_waitsForACalmMoment() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Unlock, "market"), 0);
            OverlayContext busy = new OverlayContext { Busy = true };
            OverlayContext danger = new OverlayContext { Danger = true };
            queue.update(1, busy);
            Assert.Null(queue.getCurrent());
            queue.update(2, danger);
            Assert.Null(queue.getCurrent());
            queue.update(3, OverlayContext.Calm);
            Assert.Equal("market", queue.getCurrent().Key);
        }

        [Fact]
        public void anUrgentItem_showsAtOnce_andPushesALesserOneBack() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Tip, "tip", 0), 0);
            queue.update(1, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
            queue.enqueue(item(OverlayKind.Event, "wanderers", 0), 2);
            // not calm: the event opens anyway
            queue.update(2, new OverlayContext { Busy = true });
            Assert.Equal("wanderers", queue.getCurrent().Key);
            Assert.True(queue.contains("tip"));
            queue.dismiss(3);
            queue.update(3 + OverlayQueue.CalmGapSeconds, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
        }

        [Fact]
        public void aHigherButNotUrgentItem_doesNotCutOffTheCardBeingRead() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Tip, "tip", 0), 0);
            queue.update(1, OverlayContext.Calm);
            queue.enqueue(item(OverlayKind.Unlock, "laws", 0), 1.5);
            queue.update(2, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
        }

        [Fact]
        public void whenTheMomentStopsBeingCalm_aShowingCardStepsAside_andComesBack() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Unlock, "trade", 0), 0);
            queue.update(1, OverlayContext.Calm);
            queue.update(2, new OverlayContext { Busy = true });
            Assert.Null(queue.getCurrent());
            Assert.True(queue.contains("trade"));
            queue.update(3, OverlayContext.Calm);
            Assert.Equal("trade", queue.getCurrent().Key);
        }

        [Fact]
        public void cardsNeverFollowBackToBack_thereIsACalmGap() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Toast, "a"), 0);
            queue.enqueue(item(OverlayKind.Toast, "b"), 0);
            queue.update(0, OverlayContext.Calm);
            Assert.Equal("a", queue.getCurrent().Key);
            // a toast expires on its own
            queue.update(OverlayQueue.ToastSeconds, OverlayContext.Calm);
            Assert.Null(queue.getCurrent());
            queue.update(OverlayQueue.ToastSeconds + OverlayQueue.CalmGapSeconds / 2, OverlayContext.Calm);
            Assert.Null(queue.getCurrent());
            queue.update(OverlayQueue.ToastSeconds + OverlayQueue.CalmGapSeconds + 0.01, OverlayContext.Calm);
            Assert.Equal("b", queue.getCurrent().Key);
        }

        [Fact]
        public void aToastThatWaitedTooLong_isDropped_butACardIsNot() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Toast, "old news"), 0);
            queue.enqueue(item(OverlayKind.Unlock, "card"), 0);
            OverlayContext busy = new OverlayContext { Busy = true };
            queue.update(OverlayQueue.StaleToastSeconds + 1, busy);
            Assert.False(queue.contains("old news"));
            Assert.True(queue.contains("card"));
        }

        [Fact]
        public void theSameKey_isQueuedOnce_andCanBeWithdrawn() {
            OverlayQueue queue = new OverlayQueue();
            Assert.True(queue.enqueue(item(OverlayKind.Tip, "tip:move", 0), 0));
            Assert.False(queue.enqueue(item(OverlayKind.Tip, "tip:move", 0), 1));
            queue.update(1, OverlayContext.Calm);
            Assert.False(queue.enqueue(item(OverlayKind.Tip, "tip:move", 0), 2));
            queue.withdraw("tip:move");
            Assert.Null(queue.getCurrent());
            Assert.Equal(0, queue.getQueuedCount());
        }

        [Fact]
        public void aTipThatHasShownAWhile_stepsAsideForAToast_andComesBack() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Tip, "tip", 0), 0);
            queue.update(0, OverlayContext.Calm);
            queue.enqueue(item(OverlayKind.Toast, "objective done"), 1);
            queue.update(OverlayQueue.TipYieldSeconds - 1, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
            queue.update(OverlayQueue.TipYieldSeconds, OverlayContext.Calm);
            Assert.Equal("objective done", queue.getCurrent().Key);
            Assert.True(queue.contains("tip"));
            double after = OverlayQueue.TipYieldSeconds + OverlayQueue.ToastSeconds + OverlayQueue.CalmGapSeconds + 0.01;
            queue.update(OverlayQueue.TipYieldSeconds + OverlayQueue.ToastSeconds, OverlayContext.Calm);
            queue.update(after, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
        }

        [Fact]
        public void aCardWithNoTime_staysUntilDismissed() {
            OverlayQueue queue = new OverlayQueue();
            queue.enqueue(item(OverlayKind.Tip, "tip", 0), 0);
            queue.update(0, OverlayContext.Calm);
            queue.update(1000, OverlayContext.Calm);
            Assert.Equal("tip", queue.getCurrent().Key);
            Assert.Equal("tip", queue.dismiss(1000).Key);
            Assert.Null(queue.getCurrent());
        }
    }

    public class TestOverlayPlacement {
        [Fact]
        public void aToast_goesJustUnderTheHud() {
            Assert.Equal(120f, beyondnations.desktop.ui.screens.WorldHud.overlayTop(false, 120f, 100f, 400f, 80f, 600f, 10f));
        }

        [Fact]
        public void aCard_goesWhereTheTipCardWent_aboveTheBar() {
            Assert.Equal(400f, beyondnations.desktop.ui.screens.WorldHud.overlayTop(true, 120f, 100f, 400f, 80f, 600f, 10f));
            // too low for the bar: raised to sit just above it
            Assert.Equal(510f, beyondnations.desktop.ui.screens.WorldHud.overlayTop(true, 120f, 100f, 560f, 80f, 600f, 10f));
        }

        [Fact]
        public void aCard_withNoRoom_coversTheChips_butNeverTheWarnings() {
            // the chips reach 450, the warnings 300, the bar starts at 520: a 110 tall card rises over the chips to 400
            Assert.Equal(400f, beyondnations.desktop.ui.screens.WorldHud.overlayTop(true, 450f, 300f, 200f, 110f, 520f, 10f));
            // and with no room even there it stops at the warnings' bottom
            Assert.Equal(300f, beyondnations.desktop.ui.screens.WorldHud.overlayTop(true, 450f, 300f, 200f, 300f, 520f, 10f));
        }
    }

    public class TestHudFeed {
        [Fact]
        public void theFirstLook_announcesNothing_likeALoadedSave() {
            HudFeed feed = new HudFeed();
            Assert.Null(feed.season("Autumn", 2));
            Assert.Null(feed.era("Village", new[] { "Market" }));
            Assert.Null(feed.objective(500, "Found a settlement", "+10 coins"));
            Assert.Null(feed.chronicle(40, new[] { new KeyValuePair<int, string>(40, "old news") }));
        }

        /** Stands on the land for long enough that it counts (HudFeed.LandSettleSeconds), returning the toast if any. */
        private static OverlayItem stand(HudFeed feed, ref double now, string owner, bool own, string region, string note, bool inSettlement = false) {
            now += HudFeed.LandRecentSeconds + 10;
            OverlayItem first = feed.land(owner, own, region, note, inSettlement, now);
            Assert.Null(first);
            now += HudFeed.LandSettleSeconds;
            return feed.land(owner, own, region, note, inSettlement, now);
        }

        [Fact]
        public void aBorderCrossing_isAToast_oncePerCrossing_andNotOnLoad() {
            HudFeed feed = new HudFeed();
            double now = 0;
            // the first look (a new world or a load) only takes note
            Assert.Null(feed.land("Aland", true, "Plains", "good farming and game", false, now));
            Assert.Null(stand(feed, ref now, "Aland", true, "Plains", "good farming and game"));

            OverlayItem rival = stand(feed, ref now, "Blackburg", false, "Plains", "good farming and game");
            Assert.Equal(OverlayKind.Toast, rival.Kind);
            Assert.Equal("Now on Blackburg's land", rival.Title);
            Assert.Equal(feed.getLandKey(), rival.Key);
            Assert.Null(stand(feed, ref now, "Blackburg", false, "Plains", "good farming and game"));

            OverlayItem wilds = stand(feed, ref now, null, false, "Plains", "good farming and game");
            Assert.Equal("Unclaimed land", wilds.Title);
            Assert.NotEqual(rival.Key, wilds.Key);

            OverlayItem forest = stand(feed, ref now, null, false, "Forest", "plenty of wood, little stone");
            Assert.Equal("Into the Forest", forest.Title);
            Assert.Equal("plenty of wood, little stone", forest.Body);

            // inside a settlement nothing is looked at: entering and leaving is no crossing
            Assert.Null(stand(feed, ref now, "Aland", true, "Forest", "", true));
            Assert.Equal("Back on your own land", stand(feed, ref now, "Aland", true, "Forest", "").Title);
            Assert.Null(stand(feed, ref now, "Aland", true, "Forest", "", true));
            Assert.Null(stand(feed, ref now, "Aland", true, "Forest", ""));

            // a load (reset) announces nothing again
            feed.reset();
            Assert.Null(feed.land("Blackburg", false, "Moor", "", false, now));
        }

        [Fact]
        public void aFewStepsOverABorderAndBack_sayNothing() {
            HudFeed feed = new HudFeed();
            Assert.Null(feed.land(null, false, "Plains", "", false, 0));
            // across the region border for a moment, then back
            Assert.Null(feed.land(null, false, "Forest", "", false, 1.0));
            Assert.Null(feed.land(null, false, "Forest", "", false, 1.0 + HudFeed.LandSettleSeconds / 2));
            Assert.Null(feed.land(null, false, "Plains", "", false, 1.5 + HudFeed.LandSettleSeconds / 2));
            Assert.Null(feed.land(null, false, "Plains", "", false, 20));
            // staying over the border counts once it has settled
            Assert.Null(feed.land(null, false, "Forest", "", false, 30));
            OverlayItem forest = feed.land(null, false, "Forest", "", false, 30 + HudFeed.LandSettleSeconds);
            Assert.Equal("Into the Forest", forest.Title);
        }

        [Fact]
        public void walkingAlongABorder_doesNotRepeatTheSameToasts() {
            HudFeed feed = new HudFeed();
            double t = 0;
            Assert.Null(feed.land(null, false, "Plains", "", false, t));
            int toasts = 0;
            // back and forth, each side held long enough to settle, for a minute
            for (int i = 0; i < 10; i++) {
                string region = i % 2 == 0 ? "Forest" : "Plains";
                t += 1;
                if (feed.land(null, false, region, "", false, t) != null) toasts++;
                t += HudFeed.LandSettleSeconds + 1;
                if (feed.land(null, false, region, "", false, t) != null) toasts++;
            }
            Assert.Equal(1, toasts); // only the first step into the forest
            // long after, the forest is news again
            t += HudFeed.LandRecentSeconds + 10;
            Assert.Null(feed.land(null, false, "Forest", "", false, t));
            Assert.NotNull(feed.land(null, false, "Forest", "", false, t + HudFeed.LandSettleSeconds));
        }

        [Fact]
        public void aTurningSeason_isAToast_onceEach() {
            HudFeed feed = new HudFeed();
            feed.season("Summer", 0);
            Assert.Null(feed.season("Summer", 0));
            OverlayItem autumn = feed.season("Autumn", 0);
            Assert.Equal(OverlayKind.Toast, autumn.Kind);
            Assert.Equal("Autumn has come", autumn.Title);
            Assert.Contains("Winter", autumn.Body);
            Assert.Null(feed.season("Autumn", 0));
        }

        [Fact]
        public void aNewEra_isAnUnlockCard_sayingWhatItUnlocks() {
            HudFeed feed = new HudFeed();
            feed.era("Camp", null);
            OverlayItem card = feed.era("Village", new[] { "Granary", "Market stalls" });
            Assert.Equal(OverlayKind.Unlock, card.Kind);
            Assert.Equal("A new era: Village", card.Title);
            Assert.Equal("Unlocks: Granary, Market stalls.", card.Body);
        }

        [Fact]
        public void aCompletedObjective_isAToast_withItsReward() {
            HudFeed feed = new HudFeed();
            feed.objective(-1, null, null);
            OverlayItem toast = feed.objective(120, "Gather wood", "+5 coins");
            Assert.Equal(OverlayKind.Toast, toast.Kind);
            Assert.Equal("Objective complete!", toast.Title);
            Assert.Equal("Gather wood\n+5 coins", toast.Body);
            Assert.Null(feed.objective(120, "Gather wood", "+5 coins"));
        }

        [Fact]
        public void chronicleNews_isOneToastForTheNewest_countingTheRest() {
            HudFeed feed = new HudFeed();
            feed.chronicle(10, null);
            List<KeyValuePair<int, string>> news = new List<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(11, "Ada joined your nation."),
                new KeyValuePair<int, string>(13, "Your village grew."),
            };
            OverlayItem toast = feed.chronicle(14, news);
            Assert.Equal("News (+1 more)", toast.Title);
            Assert.Equal("Your village grew.", toast.Body);
            // other nations' entries alone pass silently
            Assert.Null(feed.chronicle(16, new List<KeyValuePair<int, string>>()));
            Assert.False(feed.chronicleChanged(16));
        }

        [Fact]
        public void reset_forgetsWhatWasSeen_soALoadAnnouncesNothing() {
            HudFeed feed = new HudFeed();
            feed.season("Spring", 0);
            feed.reset();
            Assert.Null(feed.season("Winter", 3));
        }
    }

    public class TestNextLine {
        [Fact]
        public void theLine_isTheObjectiveAndItsProgress() {
            Assert.Equal("Next: Gather wood - Wood 3/10", NextLine.text("Gather wood", "Wood 3/10"));
            Assert.Equal("Next: Found a settlement", NextLine.text("Found a settlement", null));
            Assert.Null(NextLine.text(null, "x"));
        }

        [Fact]
        public void aNewWorld_saysWhatToDoFirst_withoutNamingAKey() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            string line = NextLine.of(simulation.getObjectiveTracker());
            Assert.StartsWith(NextLine.Prefix, line);
            Assert.DoesNotContain("{", line);
            Assert.DoesNotContain("Press", line);
            Assert.InRange(NextLine.fraction(simulation.getObjectiveTracker()), 0f, 1f);
        }
    }

    public class TestHudRelevance {
        [Fact]
        public void settlementAndMarket_onlyInside() {
            Assert.False(HudRelevance.showSettlement(false));
            Assert.True(HudRelevance.showSettlement(true));
            Assert.False(HudRelevance.showMarket(false, true, true));
            Assert.False(HudRelevance.showMarket(true, false, true));
            Assert.False(HudRelevance.showMarket(true, true, false));
            Assert.True(HudRelevance.showMarket(true, true, true));
        }

        [Theory]
        [InlineData(Season.SPRING, false)]
        [InlineData(Season.SUMMER, false)]
        [InlineData(Season.AUTUMN, true)]
        [InlineData(Season.WINTER, true)]
        public void winterLines_onlyInAutumnAndWinter(Season season, bool shown) {
            Assert.Equal(shown, HudRelevance.showWinterLines(season, true));
            Assert.False(HudRelevance.showWinterLines(season, false));
        }

        [Fact]
        public void theCarryingChip_onlyWhileCarrying() {
            Assert.False(HudRelevance.showCarrying(0));
            Assert.True(HudRelevance.showCarrying(1));
            Assert.Null(HudRelevance.carryingLine(new List<KeyValuePair<string, int>>()));
            Assert.Equal("Carrying 12 wood, 3 apples +2 more", HudRelevance.carryingLine(new List<KeyValuePair<string, int>> {
                new KeyValuePair<string, int>("wood", 12), new KeyValuePair<string, int>("apples", 3),
                new KeyValuePair<string, int>("stone", 2), new KeyValuePair<string, int>("saplings", 1),
            }));
        }

        [Fact]
        public void theCarryingChip_leavesCoinsToTheirMeter() {
            Inventory inventory = new Inventory(50);
            Assert.Empty(beyondnations.desktop.ui.screens.WorldHud.carried(inventory));
            inventory.addItem(ItemType.WOOD, 4);
            var carried = beyondnations.desktop.ui.screens.WorldHud.carried(inventory);
            Assert.Single(carried);
            Assert.Equal(4, carried[0].Value);
        }

        [Fact]
        public void coins_showOnceUnlockedOrHeld() {
            Assert.False(HudRelevance.showCoins(false, 0));
            Assert.True(HudRelevance.showCoins(false, 5));
            Assert.True(HudRelevance.showCoins(true, 0));
        }

        [Fact]
        public void aMeter_isLitWhenLow_orForAWhileAfterAChange() {
            MeterTracker meter = new MeterTracker();
            Assert.False(meter.update(80f, false, 0));
            Assert.True(meter.update(80f, true, 1));
            Assert.False(meter.update(80.5f, false, 2, 1f));
            Assert.True(meter.update(70f, false, 3, 1f));
            Assert.True(meter.update(70f, false, 3 + MeterTracker.ChangeSeconds - 0.1, 1f));
            Assert.False(meter.update(70f, false, 3 + MeterTracker.ChangeSeconds, 1f));
        }
    }

    public class TestStatusLine {
        [Fact]
        public void feedback_expiresInRealTime() {
            StatusLine line = new StatusLine();
            line.observe("Gathered 5 wood.", false, 10);
            Assert.Equal("Gathered 5 wood.", line.visible(10 + StatusLine.FeedbackSeconds - 0.1));
            Assert.Null(line.visible(10 + StatusLine.FeedbackSeconds));
            // the game saying the same line again does not bring it back
            line.observe("Gathered 5 wood.", false, 20);
            Assert.Null(line.visible(20));
        }

        [Fact]
        public void aRefusal_staysALittleLonger() {
            StatusLine line = new StatusLine();
            line.observe("Not enough wood.", true, 0);
            Assert.True(line.isRefusal(StatusLine.FeedbackSeconds + 0.5));
            Assert.Null(line.visible(StatusLine.RefusalSeconds));
        }

        [Fact]
        public void aHint_neverOverwritesFeedback() {
            StatusLine line = new StatusLine();
            line.setHint("A market is near: trade there.");
            Assert.Equal("A market is near: trade there.", line.visible(0));
            line.observe("Bought 2 apples.", false, 1);
            Assert.Equal("Bought 2 apples.", line.visible(1));
            Assert.Equal("A market is near: trade there.", line.visible(1 + StatusLine.FeedbackSeconds));
        }

        [Fact]
        public void aNewMessage_restartsTheClock() {
            StatusLine line = new StatusLine();
            line.observe("One.", false, 0);
            line.observe("Two.", false, 3);
            Assert.Equal("Two.", line.visible(3 + StatusLine.FeedbackSeconds - 0.1));
            Assert.Equal(0.5, line.age(3.5), 3);
        }
    }

    public class TestHudUnlockSeam {
        [Fact]
        public void allUnlocked_showsEverything_andHasNoCards() {
            IHudUnlocks unlocks = new EverythingUnlocked();
            foreach (UnlockId id in System.Enum.GetValues(typeof(UnlockId))) {
                Assert.True(unlocks.isUnlocked(id));
            }
            Assert.True(unlocks.commandUnlocked("Declare War"));
            Assert.Empty(unlocks.drainNewCards());
        }

        [Fact]
        public void aNewGameOrYoungLoad_unlocksAsItGoes_anOldLoadOrStageGetsEverything() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            beyondnations.desktop.ui.screens.WorldHud hud = new beyondnations.desktop.ui.screens.WorldHud();
            hud.beginWorld(simulation, true, false);
            Assert.False(((HudUnlocks) hud.getUnlocks()).isEverythingUnlocked());
            Assert.True(hud.getUnlocks().isUnlocked(UnlockId.Basics));
            // A load of a young Camp-era world (here at tick 0) keeps the ramp:
            // what holds now is granted silently, with no cards. An older world
            // (past the Camp era or HudUnlocks.YoungWorldSeconds) gets everything;
            // that decision is pinned by TestHudUnlocks.testYoungWorld_OnlyACampUnderTenMinutes.
            Assert.True(HudUnlocks.isYoungWorld(simulation));
            hud.beginWorld(simulation, false, false);
            Assert.False(((HudUnlocks) hud.getUnlocks()).isEverythingUnlocked());
            Assert.True(hud.getUnlocks().isUnlocked(UnlockId.Basics));
            Assert.Empty(hud.getUnlocks().drainNewCards());
            hud.beginWorld(simulation, true, true);
            Assert.True(((HudUnlocks) hud.getUnlocks()).isEverythingUnlocked());
        }
    }
}
