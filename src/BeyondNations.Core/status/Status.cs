namespace beyondnations {

    /**
    * The transient message shown to the player, and when it expires.
    *
    * Status used to own a TextGameObject, which owned a Unity Canvas and Text
    * component, so a status message could not exist without a graphics context.
    * It is now the message and its expiry and nothing else; drawing it is the
    * host's job, and moves to ImGui under #221.
    */
    public class Status {
        private TickCounter tickCounter;
        private int ticksToExpire;
        private string status;
        private int tickLastSet = 0;
        private int duplicateMessageCount = 0;

        public Status(TickCounter tickCounter, int ticksToExpire) {
            this.tickCounter = tickCounter;
            this.ticksToExpire = ticksToExpire;
            this.status = "Game started.";
        }

        public void update(string status) {
            if (getStatus().Contains(status)) {
                duplicateMessageCount += 1;
                status += " (x" + (duplicateMessageCount + 1) + ")";
            }
            else {
                duplicateMessageCount = 0;
            }
            this.status = status;
            tickLastSet = tickCounter.getTotalTicks();
        }

        public void clearStatusIfExpired() {
            if (tickCounter.getTotalTicks() - tickLastSet > ticksToExpire) {
                this.status = "";
            }
        }

        public int getTicksToExpire() {
            return ticksToExpire;
        }

        public int getTickLastSet() {
            return tickLastSet;
        }

        public string getStatus() {
            return status;
        }
    }
}
