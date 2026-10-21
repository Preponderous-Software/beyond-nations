using System.Collections.Generic;

namespace beyondnations {

    /**
    * One kind of world event: a drought, a sickness, a stranger with a map.
    *
    * An event is stateless. Everything about one happening of it -- which
    * nation, which settlement, how bad -- lives in a WorldEventInstance, which
    * is what a save holds, so an event can always be described and resolved
    * again from the instance alone after a load. New events are added by
    * implementing this and registering it with WorldEventRegistry; nothing
    * else needs to change.
    *
    * Two kinds:
    *   - personal events (isPersonal) happen to the player, whether or not
    *     they belong to a nation, and are never given to a pawn-led nation;
    *   - nation events happen to a nation. When the player leads it they
    *     choose; otherwise the nation's leader does (chooseForNation), and
    *     the outcome is news in the chronicle.
    */
    public interface IWorldEvent {
        /** A stable id, written to saves. Never rename one that has shipped. */
        string getId();

        /** The heading on the choice sheet: "Drought". */
        string getTitle();

        bool isPersonal();

        /**
        * How likely this event is for the nation (null for a personal event)
        * right now, against the other events' weights; 0 when it cannot
        * happen at all. This is where context comes in: no drought in winter,
        * no fire without a storehouse, bandits drawn to rich settlements.
        */
        int getWeight(WorldEventContext context, Nation nation);

        /**
        * Fills in who and what the event is about (settlement, other nation,
        * how bad), drawing any choice it makes from context.getRandom().
        * Returns false when, on a closer look, it cannot happen after all.
        */
        bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance);

        /** What has happened, for the choice sheet: two or three sentences. */
        string describe(WorldEventContext context, WorldEventInstance instance);

        /** Two or three options with clear trade-offs, in the order shown. */
        List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance);

        /** What happens when nobody chooses in time: always an available choice. */
        int getDefaultChoice(WorldEventContext context, WorldEventInstance instance);

        /** What a pawn-led nation does. Deterministic, from the world's state. */
        int chooseForNation(WorldEventContext context, WorldEventInstance instance);

        /**
        * Applies a choice through the existing systems and returns the
        * chronicle line saying how it turned out. The choice has been checked
        * to be available.
        */
        string resolve(WorldEventContext context, WorldEventInstance instance, int choice);
    }
}
