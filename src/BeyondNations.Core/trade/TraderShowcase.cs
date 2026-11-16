using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * The world --stage-trader opens on, for screenshots of the trader role
    * (#487) and of an alliance at +30 with a much larger nation (#478).
    *
    * It builds on DiplomacyShowcase: the player leads a Town-era nation with
    * one village and two serfs, with a friendly neighbour (+35) to the left.
    * On top of that:
    *
    * - the player's stall at home holds cheap wood, and the neighbour's
    *   stall has coin to pay for it;
    * - the first serf, with a purse, takes up trade on its own through the
    *   real behaviours (PawnBehaviorExecutor): it buys a load at home, sells
    *   it at the neighbour's market and buys the next load. Only the walk
    *   between the two markets is skipped. So the nation's one trader slot
    *   is taken and the chronicle tells the trader's doings;
    * - the second serf stands beside the player inside the village, and the
    *   player holds the stake, so the command bar's Appoint Trader is greyed
    *   only because the slot is taken;
    * - the neighbour takes in four more people, so it is more than twice the
    *   player's strength: before #478 it would have wanted +60 to ally.
    */
    public static class TraderShowcase {
        public const int HomeWood = 60;
        public const int TraderPurse = 200;
        public const int FriendlyNewcomers = 4;

        public class Staged {
            public DiplomacyShowcase.Staged diplomacy;
            public Pawn trader;
            public Pawn serf;
        }

        public static Staged stage(Simulation simulation) {
            Staged staged = new Staged();
            staged.diplomacy = DiplomacyShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            EntityRepository entities = simulation.getEntityRepository();
            NationRepository nations = simulation.getNationRepository();
            Nation own = staged.diplomacy.playerNation;
            Settlement home = staged.diplomacy.playerSettlement;
            Settlement neighbour = staged.diplomacy.friendlySettlement;
            Nation friendly = staged.diplomacy.friendly;

            List<Pawn> serfs = new List<Pawn>();
            foreach (EntityId id in own.getMemberIds()) {
                Pawn pawn = entities.getEntity(id) as Pawn;
                if (pawn != null) {
                    serfs.Add(pawn);
                }
            }
            staged.trader = serfs[0];
            staged.serf = serfs[1];

            Stall homeStall = home.getMarket().getStalls()[0];
            homeStall.setOwnerId(player.getId());
            homeStall.getInventory().addItem(ItemType.WOOD, HomeWood);
            Stall farStall = neighbour.getMarket().getStalls()[0];
            farStall.setOwnerId(friendly.getLeaderId());
            farStall.getInventory().addItem(ItemType.COIN, Stall.TILL_RESERVE + 1200);

            for (int i = 0; i < FriendlyNewcomers; i++) {
                Pawn newcomer = new Pawn(neighbour.getPosition(), simulation.getPawnNameGenerator().generate(), simulation.getRandom());
                entities.addEntity(newcomer);
                friendly.addMember(newcomer.getId());
                newcomer.setNationId(friendly.getId());
                newcomer.setColor(friendly.getColor());
                newcomer.setHomeSettlementId(neighbour.getId());
                newcomer.setEnergy(100);
                SettlementLife.admit(newcomer, neighbour);
            }

            // the trader: real buying and selling, with the walk skipped
            PawnBehaviorExecutor executor = new PawnBehaviorExecutor(simulation.getEnvironment(), nations, simulation.getEventProducer(),
                entities, simulation.getRandom(), simulation.getNationNameGenerator(), simulation.getTickCounter());
            staged.trader.getInventory().addItem(ItemType.COIN, TraderPurse);
            staged.trader.setEnergy(100);
            executor.executeBehavior(staged.trader, BehaviorType.BUY_TRADE_GOODS);
            staged.trader.setCurrentSettlementId(neighbour.getId());
            executor.executeBehavior(staged.trader, BehaviorType.SELL_RESOURCES);
            staged.trader.setCurrentSettlementId(home.getId());
            executor.executeBehavior(staged.trader, BehaviorType.BUY_TRADE_GOODS);

            // the other serf out on the road in front of the player, close
            // enough to stop for them (PawnRelations.findPawnToHold), and the
            // stake in hand
            home.removeCurrentlyPresentEntity(staged.serf.getId());
            staged.serf.setCurrentSettlementId(null);
            Vector3 forward = player.getForward();
            forward = Vector3.Normalize(new Vector3(forward.X, 0f, forward.Z));
            staged.serf.setPosition(new Vector3(player.getPosition().X, staged.serf.getPosition().Y, player.getPosition().Z) + forward * 3f);
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);
            player.getStatus().update(staged.trader.getName() + " trades for " + own.getName() + ". Members are on the nations screen.");
            return staged;
        }
    }
}
