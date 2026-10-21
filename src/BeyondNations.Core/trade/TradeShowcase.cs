using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * Stands up a trade road, so caravans and living prices can be seen (and
    * screenshotted) without an hour of play.
    *
    * The player leads a nation whose village, ahead and to the left, has a
    * Storehouse -- so it can send caravans -- and a hungry crowd. Ahead and to
    * the right is a pawn-led trading nation's village with a merchant, a full
    * purse and a woodpile it cannot use, and one of its caravans has just
    * rolled out onto the road a few steps in front of the player, bound for
    * the player's village. The player carries goods to trade.
    *
    * Used by the host's --stage-trade flag and by tests. Deterministic for a
    * given world seed.
    */
    public static class TradeShowcase {
        public const float HalfGap = 160f;
        public const float AheadDistance = 140f;
        public const int TraderMembers = 4;
        public const int PlayerPawns = 6;

        public class Staged {
            public Nation playerNation;
            public Nation traders;
            public Settlement playerSettlement;
            public Settlement traderSettlement;
            public Caravan caravan;
        }

        public static Staged stage(Simulation simulation) {
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            RandomSource random = simulation.getRandom();
            Staged staged = new Staged();

            simulation.getWorldGenerator().update();

            new NationCreateCommand(nationRepository, simulation.getEventProducer(), random, simulation.getNationNameGenerator())
                .execute(player);
            staged.playerNation = nationRepository.getNation(player.getNationId());

            Vector3 start = player.getPosition();
            Vector3 forward = player.getForward();
            forward = Vector3.Normalize(new Vector3(forward.X, 0f, forward.Z));
            Vector3 right = new Vector3(-forward.Z, 0f, forward.X);

            staged.playerSettlement = place(simulation, staged.playerNation, start - right * HalfGap + forward * AheadDistance, 3);
            player.setHomeSettlementId(staged.playerSettlement.getId());
            addPawns(simulation, staged.playerNation, staged.playerSettlement, PlayerPawns);
            // a treasury to buy the caravan's goods with
            staged.playerSettlement.addFunds(300);

            // the traders: a pawn-led nation with a merchant and wood to spare
            Vector3 site = start + right * HalfGap + forward * AheadDistance;
            Pawn leader = new Pawn(site, simulation.getPawnNameGenerator().generate(), random);
            leader.setTraits(PawnTraits.roll(simulation.getTraitRandom()));
            simulation.getEntityRepository().addEntity(leader);
            staged.traders = new Nation(simulation.getNationNameGenerator().generate(), leader.getId(), random);
            nationRepository.addNation(staged.traders);
            leader.setNationId(staged.traders.getId());
            leader.setColor(staged.traders.getColor());
            simulation.getEventProducer().produceNationCreationEvent(staged.traders);
            staged.traderSettlement = place(simulation, staged.traders, site, 1);
            leader.setHomeSettlementId(staged.traderSettlement.getId());
            SettlementLife.admit(leader, staged.traderSettlement);
            addPawns(simulation, staged.traders, staged.traderSettlement, TraderMembers - 1);
            Stall stall = staged.traderSettlement.getMarket().getStallForSale();
            stall.setOwnerId(leader.getId());
            staged.traders.setRole(leader.getId(), NationRole.LEADER);
            stall.getInventory().addItem(ItemType.COIN, 400);
            stall.getInventory().addItem(ItemType.WOOD, 30);
            staged.traderSettlement.getInventory().addItem(ItemType.WOOD, 90);
            staged.traderSettlement.getInventory().addItem(ItemType.STONE, 50);
            staged.traderSettlement.addFunds(60);

            nationRepository.getRelations().adjust(staged.playerNation, staged.traders, 25);

            // prices settle toward what each village has and needs
            for (int i = 0; i < 12; i++) {
                MarketPricing.runRound(simulation.getEntityRepository(), simulation.getTradeCaravans().getNeeds());
            }

            // one of their caravans, just out on the road ahead of the player
            TradeCaravans trade = simulation.getTradeCaravans();
            List<ItemStack> cargo = TradeCaravans.pickCargo(staged.traderSettlement, TradeCaravans.MAX_CARGO, trade.getNeeds());
            staged.caravan = trade.sendCaravan(staged.traders, staged.traderSettlement, staged.playerSettlement, cargo,
                simulation.getTickCounter().getTotalTicks());
            staged.caravan.setPosition(new Vector3(start.X + forward.X * 7f, 0f, start.Z + forward.Z * 7f));

            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 10);
            player.getInventory().addItem(ItemType.APPLE, 6);
            player.getInventory().addItem(ItemType.COIN, 200);
            player.getStatus().update("A " + staged.traders.getName() + " caravan is on the road ahead. Trade with it, guard it, or rob it.");
            return staged;
        }

        private static Settlement place(Simulation simulation, Nation nation, Vector3 site, int buildings) {
            Vector3 at = new Vector3(site.X, simulation.getPlayer().getPosition().Y, site.Z);
            Settlement settlement = new Settlement(at, nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            BuildingType[] order = { BuildingType.STOREHOUSE, BuildingType.HOUSE, BuildingType.FARM };
            for (int i = 0; i < buildings; i++) {
                settlement.addBuilding(order[i % order.Length]);
            }
            settlement.getMarket().createStall();
            Territory.updateClaims(simulation.getEntityRepository());
            return settlement;
        }

        private static void addPawns(Simulation simulation, Nation nation, Settlement settlement, int count) {
            for (int i = 0; i < count; i++) {
                Pawn pawn = new Pawn(settlement.getPosition(), simulation.getPawnNameGenerator().generate(), simulation.getRandom());
                pawn.setTraits(PawnTraits.roll(simulation.getTraitRandom()));
                simulation.getEntityRepository().addEntity(pawn);
                nation.addMember(pawn.getId());
                pawn.setNationId(nation.getId());
                pawn.setColor(nation.getColor());
                pawn.setHomeSettlementId(settlement.getId());
                pawn.setEnergy(100);
                SettlementLife.admit(pawn, settlement);
            }
            settlement.setResidentCount(SettlementLife.countResidents(simulation.getEntityRepository(), settlement.getId()));
        }
    }
}
