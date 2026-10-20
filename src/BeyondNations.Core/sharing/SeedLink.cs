namespace beyondnations {

    /**
    * The link that opens a world in the browser build:
    * https://beyond-nations.play.danielstephenson.dev/?seed=42. The page reads
    * ?seed= (WebOptions) and the New World screen opens with that seed filled
    * in, so whoever follows the link starts the same world.
    *
    * The seed in a link is always the number, even for a world named with
    * text: the number is what the world is built from, and it survives being
    * pasted anywhere a URL goes.
    */
    public static class SeedLink {
        public const string BaseUrl = "https://beyond-nations.play.danielstephenson.dev/";

        public static string build(int seed) {
            return BaseUrl + "?seed=" + WorldSeed.format(seed);
        }
    }
}
