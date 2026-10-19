using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A save must hold every piece of world state. This walks every type a
    * Simulation is made of and fails on any field SaveCoverage does not
    * account for -- which is what happens the day someone adds a field to
    * Pawn, or a new kind of entity, without saving it.
    */
    public class TestSaveCoverage {

        [Fact]
        public void testEveryFieldOfEveryWorldType_IsSavedDerivedOrTransient() {
            StringBuilder problems = new StringBuilder();
            foreach (Type type in SaveCoverage.reachableTypes()) {
                List<string> fields = SaveCoverage.declaredFields(type);
                if (SaveCoverage.isEventPayload(type)) {
                    continue;
                }
                SaveCoverage.Entry entry;
                if (!SaveCoverage.Manifest.TryGetValue(type, out entry)) {
                    if (fields.Count > 0 || !type.IsAbstract) {
                        problems.Append("type ").Append(type.Name).Append(" (fields: ").Append(string.Join(", ", fields))
                            .Append(") is part of the world but not in SaveCoverage.Manifest\n");
                    }
                    continue;
                }
                foreach (string field in fields) {
                    bool listed = Array.IndexOf(entry.Saved, field) >= 0 || Array.IndexOf(entry.Derived, field) >= 0
                        || Array.IndexOf(entry.Transient, field) >= 0;
                    if (!listed) {
                        problems.Append(type.Name).Append('.').Append(field)
                            .Append(" is not saved: write it in ").Append(type.Name).Append(".save, read it back, and list it in SaveCoverage\n");
                    }
                }
                foreach (string[] list in new[] { entry.Saved, entry.Derived, entry.Transient }) {
                    foreach (string field in list) {
                        if (!fields.Contains(field)) {
                            problems.Append("SaveCoverage lists ").Append(type.Name).Append('.').Append(field).Append(", which no longer exists\n");
                        }
                    }
                }
            }
            Assert.True(problems.Length == 0, problems.ToString());
        }

        [Fact]
        public void testManifest_HasNoTypeThatIsNoLongerPartOfTheWorld() {
            List<Type> reachable = SaveCoverage.reachableTypes();
            foreach (Type type in SaveCoverage.Manifest.Keys) {
                Assert.True(reachable.Contains(type), type.Name + " is in SaveCoverage but a Simulation no longer holds one");
            }
        }

        [Fact]
        public void testEveryEntityType_CanBeSaved() {
            // every concrete Entity has a shell in Simulation.createShell; a
            // new kind of entity without one fails the round trip, but say so here first
            foreach (EntityType type in Enum.GetValues(typeof(EntityType))) {
                if (type == EntityType.NONE) {
                    continue;
                }
                Assert.Contains(type.ToString(), new[] { "TREE", "ROCK", "PAWN", "PLAYER", "SETTLEMENT", "SAPLING", "ITEM_DROP", "CHICKEN" });
            }
        }
    }
}
