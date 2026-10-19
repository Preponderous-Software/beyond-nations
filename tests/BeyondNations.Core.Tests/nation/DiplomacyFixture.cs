using System.Numerics;

using beyondnations;

namespace beyondnationstests {

    /**
    * A bare world for diplomacy tests: a Simulation that is never ticked, so
    * nothing but what a test puts in it happens, and helpers to found
    * pawn-led nations and settlements at chosen places.
    */
    public class DiplomacyFixture {
        public readonly Simulation simulation;
        public readonly NationRepository nations;
        public readonly EntityRepository entities;
        public readonly NationRelations relations;
        public readonly Diplomacy diplomacy;
        public readonly Player player;

        public DiplomacyFixture() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            simulation = new Simulation(config);
            nations = simulation.getNationRepository();
            entities = simulation.getEntityRepository();
            relations = nations.getRelations();
            diplomacy = simulation.getDiplomacy();
            player = simulation.getPlayer();
        }

        /** A nation led by a pawn, with members-1 more pawns, and no settlement. */
        public Nation pawnNation(string name, int members) {
            Pawn leader = pawn();
            Nation nation = new Nation(name, leader.getId(), simulation.getRandom());
            nations.addNation(nation);
            leader.setNationId(nation.getId());
            for (int i = 1; i < members; i++) {
                addMember(nation);
            }
            return nation;
        }

        /** The player's own nation, led by the player, with members-1 pawns. */
        public Nation playerNation(int members) {
            new NationCreateCommand(nations, simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
            Nation nation = nations.getNation(player.getNationId());
            for (int i = 1; i < members; i++) {
                addMember(nation);
            }
            return nation;
        }

        public Pawn addMember(Nation nation) {
            Pawn member = pawn();
            nation.addMember(member.getId());
            member.setNationId(nation.getId());
            return member;
        }

        public Pawn pawn() {
            Pawn pawn = new Pawn(Vector3.Zero, simulation.getPawnNameGenerator().generate(), simulation.getRandom());
            entities.addEntity(pawn);
            return pawn;
        }

        public Settlement settlement(Nation nation, float x, float z, int buildings = 0) {
            Settlement settlement = new Settlement(new Vector3(x, 1, z), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            entities.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            BuildingType[] order = { BuildingType.HOUSE, BuildingType.FARM, BuildingType.STOREHOUSE, BuildingType.HOUSE, BuildingType.FARM };
            for (int i = 0; i < buildings; i++) {
                settlement.addBuilding(order[i]);
            }
            Territory.updateClaims(entities);
            return settlement;
        }

        public void rounds(int count) {
            for (int i = 0; i < count; i++) {
                diplomacy.runRound();
            }
        }
    }
}
