using System.Numerics;

namespace beyondnations {

    /**
    * Stands a lived-in settlement up in front of the player, so that what a
    * town looks like can be seen (and screenshotted) without an hour of play.
    *
    * The player founds a nation and a settlement a short walk ahead of where
    * they face; the market is filled with stalls, some owned by merchants; a
    * handful of pawns join the nation, call the settlement home and are inside
    * it. Everything goes through the same commands and behaviours play uses,
    * so the result is a state the game can reach on its own. The
    * buildings are put up directly, as if a leader had already paid for them.
    *
    * Used by the host's --stage-settlement flag and by tests. Deterministic for
    * a given world seed.
    */
    public static class SettlementShowcase {

        public const float DistanceAhead = 17f;
        public const int NumResidents = 8;
        public const int NumMerchants = 3;

        /** Enough to make it a town, with one of everything. */
        public static readonly BuildingType[] Buildings = {
            BuildingType.HOUSE, BuildingType.FARM, BuildingType.STOREHOUSE, BuildingType.HOUSE, BuildingType.FARM
        };

        public static Settlement stage(Simulation simulation) {
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            EntityRepository entityRepository = simulation.getEntityRepository();
            RandomSource random = simulation.getRandom();

            new NationCreateCommand(nationRepository, simulation.getEventProducer(), random, simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = nationRepository.getNation(player.getNationId());

            // Found the settlement ahead of the player, then step back to where
            // they were, so the trailing camera looks at it.
            Vector3 start = player.getPosition();
            Vector3 site = start + player.getForward() * DistanceAhead;
            player.setPosition(new Vector3(site.X, start.Y, site.Z));
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(nationRepository, simulation.getEventProducer(), entityRepository, simulation.getGameConfig(), random)
                .execute(player);
            player.setPosition(start);

            Settlement settlement = (Settlement) entityRepository.getEntity(player.getHomeSettlementId());
            foreach (BuildingType type in Buildings) {
                settlement.addBuilding(type);
            }
            // a stockpile delivered by ordered pawns, piled on the plaza
            settlement.getInventory().addItem(ItemType.WOOD, 40);
            settlement.getInventory().addItem(ItemType.STONE, 25);
            Market market = settlement.getMarket();
            while (market.createStall()) {
            }

            for (int i = 0; i < NumResidents; i++) {
                Pawn pawn = new Pawn(settlement.getPosition(), simulation.getPawnNameGenerator().generate(), random);
                pawn.setTraits(PawnTraits.roll(simulation.getTraitRandom()));
                entityRepository.addEntity(pawn);
                nation.addMember(pawn.getId());
                pawn.setNationId(nation.getId());
                pawn.setColor(nation.getColor());
                pawn.setHomeSettlementId(settlement.getId());
                pawn.setEnergy(100);

                if (i < NumMerchants) {
                    Stall stall = market.getStallForSale();
                    if (stall != null) {
                        stall.setOwnerId(pawn.getId());
                        nation.setRole(pawn.getId(), NationRole.MERCHANT);
                        stall.getInventory().addItem(ItemType.APPLE, 10);
                    }
                }

                // scattered about the plaza, as if they had been milling a while
                float angle = i * 2.4f;
                float radius = SettlementLayout.MillMinRadius + (i * 5 % 11);
                pawn.setPosition(settlement.getPosition() + new Vector3(System.MathF.Cos(angle) * radius, 0, System.MathF.Sin(angle) * radius));
                SettlementLife.admit(pawn, settlement);
            }
            settlement.setResidentCount(SettlementLife.countResidents(entityRepository, settlement.getId()));
            settlement.refreshAppearance();
            return settlement;
        }
    }
}
