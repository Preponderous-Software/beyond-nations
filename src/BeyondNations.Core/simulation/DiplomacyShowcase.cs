using System.Numerics;

namespace beyondnations {

    /**
    * Stands up a small corner of a world at odds with itself, so that borders,
    * relations and a siege can be seen (and screenshotted) without an hour of
    * play.
    *
    * The player leads a nation with a village ahead and to the left. Ahead
    * and to the right, its border touching the player's, is the village of
    * a larger pawn-led nation that is hostile and has begun a siege; further
    * left is a friendly neighbour. The player stands in the wilds where the
    * two hostile borders meet a little way up the road.
    *
    * The villages are put closer together than founding allows, so their
    * claims meet in front of the player. Everything else -- the relations,
    * the siege -- is what Diplomacy does with that arrangement.
    *
    * Used by the host's --stage-diplomacy flag and by tests. Deterministic for
    * a given world seed.
    */
    public static class DiplomacyShowcase {
        public const float HalfGap = 100f;
        public const float AheadDistance = 120f;
        public const float FriendlyOffset = 260f;
        public const int HostileMembers = 7;
        public const int FriendlyMembers = 3;
        public const int PlayerPawns = 2;
        public const int HostileScore = -60;
        public const int FriendlyScore = 35;

        public class Staged {
            public Nation playerNation;
            public Nation hostile;
            public Nation friendly;
            public Settlement playerSettlement;
            public Settlement hostileSettlement;
            public Settlement friendlySettlement;
        }

        public static Staged stage(Simulation simulation) {
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            RandomSource random = simulation.getRandom();
            Staged staged = new Staged();

            new NationCreateCommand(nationRepository, simulation.getEventProducer(), random, simulation.getNationNameGenerator())
                .execute(player);
            staged.playerNation = nationRepository.getNation(player.getNationId());

            Vector3 start = player.getPosition();
            Vector3 forward = player.getForward();
            forward = Vector3.Normalize(new Vector3(forward.X, 0f, forward.Z));
            Vector3 right = new Vector3(-forward.Z, 0f, forward.X);

            // the two villages stand ahead, to either side, so their borders
            // meet on the road in front of the player
            staged.playerSettlement = place(simulation, staged.playerNation, start - right * HalfGap + forward * AheadDistance, 2);
            player.setHomeSettlementId(staged.playerSettlement.getId());
            addPawns(simulation, staged.playerNation, staged.playerSettlement, PlayerPawns);

            staged.hostile = foundPawnNation(simulation, start + right * HalfGap + forward * AheadDistance, HostileMembers, 2, out staged.hostileSettlement);
            staged.friendly = foundPawnNation(simulation, start - right * (HalfGap + FriendlyOffset) + forward * AheadDistance, FriendlyMembers, 0, out staged.friendlySettlement);

            NationRelations relations = nationRepository.getRelations();
            relations.adjust(staged.playerNation, staged.hostile, HostileScore);
            relations.adjust(staged.playerNation, staged.friendly, FriendlyScore);

            // two rounds: claims are drawn, and the siege is under way
            simulation.getDiplomacy().runRound();
            simulation.getDiplomacy().runRound();

            player.getStatus().update(staged.hostile.getName() + " is besieging your village. Press O to see the nations.");
            return staged;
        }

        private static Nation foundPawnNation(Simulation simulation, Vector3 site, int members, int buildings, out Settlement settlement) {
            RandomSource random = simulation.getRandom();
            Pawn leader = new Pawn(site, simulation.getPawnNameGenerator().generate(), random);
            simulation.getEntityRepository().addEntity(leader);
            Nation nation = new Nation(simulation.getNationNameGenerator().generate(), leader.getId(), random);
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());
            leader.setColor(nation.getColor());
            simulation.getEventProducer().produceNationCreationEvent(nation);

            settlement = place(simulation, nation, site, buildings);
            leader.setHomeSettlementId(settlement.getId());
            SettlementLife.admit(leader, settlement);
            addPawns(simulation, nation, settlement, members - 1);
            return nation;
        }

        private static Settlement place(Simulation simulation, Nation nation, Vector3 site, int buildings) {
            Vector3 at = new Vector3(site.X, simulation.getPlayer().getPosition().Y, site.Z);
            Settlement settlement = new Settlement(at, nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            BuildingType[] order = { BuildingType.HOUSE, BuildingType.FARM, BuildingType.STOREHOUSE };
            for (int i = 0; i < buildings; i++) {
                settlement.addBuilding(order[i % order.Length]);
            }
            settlement.getMarket().createStall();
            return settlement;
        }

        private static void addPawns(Simulation simulation, Nation nation, Settlement settlement, int count) {
            for (int i = 0; i < count; i++) {
                Pawn pawn = new Pawn(settlement.getPosition(), simulation.getPawnNameGenerator().generate(), simulation.getRandom());
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
