using System.Collections.Generic;

namespace beyondnations.desktop.ui.hud {

    /**
    * What can open over play, highest first. When two want the screen the
    * higher one wins (Primordial Rise's overlays.js): the end of a run, a
    * world event's card, an unlock card (a new era is one), an onboarding
    * tip, then a toast (an objective done, a season turning, chronicle news).
    */
    public enum OverlayKind { End = 0, Event = 1, Unlock = 2, Tip = 3, Toast = 4 }

    /** One thing to show over play. Seconds 0 stays until dismissed (Got it). */
    public sealed class OverlayItem {
        public OverlayKind Kind { get; }
        /** What it is, so the same thing queued twice shows once ("tip:move", "season:autumn:1"). */
        public string Key { get; }
        public string Title { get; }
        public string Body { get; }
        public double Seconds { get; }
        /** When it was queued, to drop a toast that has waited too long to be news. */
        public double QueuedAt { get; internal set; }

        public OverlayItem(OverlayKind kind, string key, string title, string body, double seconds) {
            Kind = kind;
            Key = key;
            Title = title;
            Body = body;
            Seconds = seconds;
        }

        /** A toast: about three seconds, then gone (Forward Two's hud.js). */
        public static OverlayItem toast(string key, string title, string body) {
            return new OverlayItem(OverlayKind.Toast, key, title, body, OverlayQueue.ToastSeconds);
        }

        /** An unlock card: up until Got it, or a while if left alone. */
        public static OverlayItem unlock(string key, string title, string body) {
            return new OverlayItem(OverlayKind.Unlock, key, title, body, OverlayQueue.CardSeconds);
        }

        public override string ToString() { return Kind + ":" + Key; }
    }

    /**
    * What is going on, for whether the moment is calm enough for a card:
    * Busy while a panel or sheet is open over the HUD (the More sheet, the
    * market or caravan panel, the full chronicle) or a world event waits on
    * the player; Danger while the player is starving or a settlement of
    * theirs is under siege.
    */
    public struct OverlayContext {
        public bool Busy;
        public bool Danger;

        public static OverlayContext Calm => new OverlayContext();
    }

    /**
    * One overlay at a time over the world HUD, by priority (OverlayKind),
    * each waiting for a calm moment (Primordial Rise's overlays.js, ported).
    *
    * - At most one item shows. When nothing shows, the highest queued item
    *   (oldest first among equals) opens, once the moment is calm (no panel
    *   open, no danger) and CalmGapSeconds have passed since the last one
    *   closed, so cards never follow each other back to back.
    * - The end of a run and a world event are urgent: they open at once,
    *   calm or not, and push a lesser item that is showing back into the
    *   queue (it shows again later, from the start).
    * - Anything else that is showing waits its turn: a newer, higher item
    *   does not cut off a card the player is reading.
    * - When the moment stops being calm, a showing item that is not urgent
    *   steps aside back into the queue.
    * - A tip stays until it is followed or dismissed, so after showing for
    *   TipYieldSeconds it steps aside for a waiting toast (an objective done
    *   must not be lost behind "Walk and look"), and comes back after it.
    * - A toast that has waited StaleToastSeconds is dropped: it was news,
    *   and its history is in the Journal's Chronicle and Goals tabs.
    * - The same Key is never queued twice while it waits or shows.
    *
    * Pure: the clock and the context are passed in, so it is checked by unit
    * tests (TestOverlayQueue) without a window.
    */
    public class OverlayQueue {
        public const double ToastSeconds = 3.5;
        public const double CardSeconds = 12.0;
        public const double CalmGapSeconds = 0.6;
        public const double StaleToastSeconds = 20.0;
        public const double TipYieldSeconds = 6.0;

        private readonly List<OverlayItem> queued = new List<OverlayItem>();
        private OverlayItem current;
        private double shownAt;
        private double lastClosedAt = double.NegativeInfinity;

        public OverlayItem getCurrent() { return current; }

        /** How long the current item has shown, in seconds (0 with none). */
        public double getShownFor(double now) { return current == null ? 0 : now - shownAt; }

        public int getQueuedCount() { return queued.Count; }

        public IReadOnlyList<OverlayItem> getQueued() { return queued; }

        public static bool isUrgent(OverlayKind kind) {
            return kind == OverlayKind.End || kind == OverlayKind.Event;
        }

        public static bool isCalm(OverlayContext context) {
            return !context.Busy && !context.Danger;
        }

        /** Whether an item with this key waits or shows. */
        public bool contains(string key) {
            if (current != null && current.Key == key) {
                return true;
            }
            foreach (OverlayItem item in queued) {
                if (item.Key == key) {
                    return true;
                }
            }
            return false;
        }

        /** Queues an item; false when one with its key already waits or shows. */
        public bool enqueue(OverlayItem item, double now) {
            if (item == null || contains(item.Key)) {
                return false;
            }
            item.QueuedAt = now;
            queued.Add(item);
            return true;
        }

        /** Takes an item back, waiting or showing (a tip the player has already followed). */
        public void withdraw(string key) {
            queued.RemoveAll(item => item.Key == key);
            if (current != null && current.Key == key) {
                current = null;
            }
        }

        /** The player closed the item showing (Got it, or a tap on a toast). */
        public OverlayItem dismiss(double now) {
            OverlayItem closed = current;
            if (current != null) {
                current = null;
                lastClosedAt = now;
            }
            return closed;
        }

        /** Advances the queue to now: expires, steps aside, drops stale toasts, opens the next. */
        public void update(double now, OverlayContext context) {
            bool calm = isCalm(context);
            if (current != null && current.Seconds > 0 && now - shownAt >= current.Seconds) {
                current = null;
                lastClosedAt = now;
            }
            queued.RemoveAll(item => item.Kind == OverlayKind.Toast && now - item.QueuedAt >= StaleToastSeconds);

            OverlayItem next = highest();
            if (current != null) {
                bool displaced = next != null && isUrgent(next.Kind) && next.Kind < current.Kind;
                bool stepAside = !calm && !isUrgent(current.Kind);
                OverlayItem toast = current.Kind == OverlayKind.Tip && now - shownAt >= TipYieldSeconds ? oldest(OverlayKind.Toast) : null;
                if (!displaced && !stepAside && toast == null) {
                    return;
                }
                // back into the queue, to show again from the start
                queued.Insert(0, current);
                current = null;
                next = displaced || stepAside ? highest() : toast;
                if (toast != null && !displaced && !stepAside) {
                    // straight from the tip to the toast: no gap, the screen was not empty
                    queued.Remove(toast);
                    current = toast;
                    shownAt = now;
                    return;
                }
            }
            if (next == null) {
                return;
            }
            if (!isUrgent(next.Kind) && (!calm || now - lastClosedAt < CalmGapSeconds)) {
                return;
            }
            queued.Remove(next);
            current = next;
            shownAt = now;
        }

        /** The oldest queued item of a kind, or null. */
        private OverlayItem oldest(OverlayKind kind) {
            foreach (OverlayItem item in queued) {
                if (item.Kind == kind) {
                    return item;
                }
            }
            return null;
        }

        /** The highest queued item, the oldest among equals; null when none waits. */
        private OverlayItem highest() {
            OverlayItem best = null;
            foreach (OverlayItem item in queued) {
                if (best == null || item.Kind < best.Kind) {
                    best = item;
                }
            }
            return best;
        }
    }
}
