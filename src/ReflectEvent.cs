using System;
using System.Reflection;
using SwiftlyS2.Shared.GameEventDefinitions;
namespace SwiftlyS2_Deathmatch {
    public static class ReflectEvent {
        public static void Dump() {
            var t = typeof(EventPlayerDeath);
            foreach (var m in t.GetMethods()) {
                Console.WriteLine("METHOD: " + m.Name);
            }
            Console.WriteLine("BASE IGameEvent:");
            foreach (var m in typeof(SwiftlyS2.Shared.GameEvents.IGameEvent<EventPlayerDeath>).GetMethods()) {
                Console.WriteLine("METHOD: " + m.Name);
            }
        }
    }
}
