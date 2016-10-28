using Xunit;

// NOTE(tim): Most of these tests reset a database to a clean state
// and we can't have tests stomping all over each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MembershipReboot.Dapper.Tests {
    public class Program {
        static void Main() {

        }
    }
}
