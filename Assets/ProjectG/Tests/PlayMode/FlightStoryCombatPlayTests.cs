using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectG.Tests
{
    public sealed class FlightStoryCombatPlayTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator NonlethalGaugeSuppressesWithoutAnyDefeatedEvent()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/FlightPrototype.unity");
            yield return null;
            GameObject pilot = GameObject.Find("Player");
            if (pilot == null) pilot = UnityEngine.Object.FindFirstObjectByType(Type.GetType("ProjectG.FlightPilotVitals, Assembly-CSharp", true)) is Component found ? found.gameObject : null;
            Component vitals = pilot.GetComponent("FlightPilotVitals");
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false;
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false;
            Component template = GameObject.Find("GlassfinEnemy").GetComponent("FlightEnemy");
            Component pool = GameObject.Find("ThreatPool").GetComponent("FlightThreatPool");
            Type adapter = Type.GetType("ProjectG.FlightStoryCombatant, Assembly-CSharp", true);
            var anchor = new GameObject("StoryTestAnchor");
            anchor.transform.position = new Vector3(0, 30, 0);
            Component actor = (Component)adapter.GetMethod("Create").Invoke(null, new object[] { template, "B-KARDEN", pool, vitals, anchor.transform, new Vector3(0, 4, 44) });
            try
            {
                Component enemy = (Component)adapter.GetProperty("Enemy").GetValue(actor);
                object gauge = adapter.GetField("gauge", Hidden).GetValue(actor);
                int defeats = 0;
                Action increment = () => defeats++;
                EventInfo defeated = enemy.GetType().GetEvent("Defeated");
                Delegate callback = MakeCallback(defeated.EventHandlerType, increment);
                defeated.AddEventHandler(enemy, callback);
                for (int phase = 0; phase < 3; phase++)
                {
                    gauge.GetType().GetProperty("ProtectedUntil").GetSetMethod(true).Invoke(gauge, new object[] { 0d });
                    enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 100000 });
                }
                Assert.That(adapter.GetProperty("IsSuppressed").GetValue(actor), Is.True);
                Assert.That(enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(0));
                Assert.That(enemy.GetType().GetProperty("IsCombatTarget").GetValue(enemy), Is.False);
                Assert.That(actor.gameObject.activeSelf, Is.True, "Karden remains alive for the rescue scene");
                Assert.That(defeats, Is.Zero, "XP/drop/energy/score consumers must remain silent");
                enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 100000 });
                actor.gameObject.SetActive(false);
                actor.gameObject.SetActive(true);
                Assert.That(enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(0), "same instance must not refill its gauge");
                Assert.That(defeats, Is.Zero);
            }
            finally { UnityEngine.Object.Destroy(actor.gameObject); UnityEngine.Object.Destroy(anchor); }
        }

        [UnityTest]
        public IEnumerator StoryBatchRefusesPartialFansAndClearCancelsSplitChildren()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/FlightPrototype.unity");
            yield return null;
            Component pool = GameObject.Find("ThreatPool").GetComponent("FlightThreatPool");
            Type assemblyType = Type.GetType("ProjectG.FlightStoryCombatProfiles, Assembly-CSharp", true);
            Type patternRules = assemblyType.Assembly.GetType("ProjectG.FlightStoryPatternRules");
            Type shotType = assemblyType.Assembly.GetType("ProjectG.FlightStoryThreatShot");
            Component pilot = UnityEngine.Object.FindFirstObjectByType(assemblyType.Assembly.GetType("ProjectG.FlightPilotVitals")) as Component;
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false;
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false;
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null);
            var fire = pool.GetType().GetMethod("TryFire", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int) });
            for (int i = 0; i < 90; i++) fire.Invoke(pool, new object[] { new Vector3(25, 30, 0), Vector3.forward, 1f, 10 });
            var ray = (IList)assemblyType.GetMethod("PatternsFor").Invoke(null, new object[] { "B-RAY" });
            var plans = (IEnumerable)patternRules.GetMethod("Build").Invoke(null, new[] { ray[0], (object)0 });
            object shots = MakeShots(shotType, plans);
            Assert.That(pool.GetType().GetMethod("TryFireStoryBurst", Hidden).Invoke(pool, new[] { shots }), Is.False);
            Assert.That(pool.GetType().GetProperty("ActiveCount").GetValue(pool), Is.EqualTo(90));
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null);
            Array invalid = (Array)MakeShots(shotType, plans);
            shotType.GetField("Direction").SetValue(invalid.GetValue(invalid.Length - 1), Vector3.zero);
            Assert.That(pool.GetType().GetMethod("TryFireStoryBurst", Hidden).Invoke(pool, new object[] { invalid }), Is.False);
            Assert.That(pool.GetType().GetProperty("ActiveCount").GetValue(pool), Is.EqualTo(0), "invalid last shot must not leave the earlier six alive");
            var varkan = (IList)assemblyType.GetMethod("PatternsFor").Invoke(null, new object[] { "B-VARKAN" });
            plans = (IEnumerable)patternRules.GetMethod("Build").Invoke(null, new[] { varkan[1], (object)0 });
            Assert.That(pool.GetType().GetMethod("TryFireStoryBurst", Hidden).Invoke(pool, new[] { MakeShots(shotType, plans) }), Is.True);
            Assert.That(pool.GetType().GetProperty("ReservedStoryCount").GetValue(pool), Is.EqualTo(9));
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null);
            yield return new WaitForSeconds(1f);
            Assert.That(pool.GetType().GetProperty("ActiveCount").GetValue(pool), Is.EqualTo(0));
            Assert.That(pool.GetType().GetProperty("ReservedStoryCount").GetValue(pool), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StoryRayUsesHalfControlAndStoppedLivingTargetsCannotBeSelected()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/FlightPrototype.unity");
            yield return null;
            Type adapter = Type.GetType("ProjectG.FlightStoryCombatant, Assembly-CSharp", true);
            Component pilot = UnityEngine.Object.FindFirstObjectByType(adapter.Assembly.GetType("ProjectG.FlightPilotVitals")) as Component;
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false;
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false;
            Component template = GameObject.Find("GlassfinEnemy").GetComponent("FlightEnemy");
            Component pool = GameObject.Find("ThreatPool").GetComponent("FlightThreatPool");
            var anchor = new GameObject("StoryControlAnchor");
            anchor.transform.position = Vector3.up * 30f;
            Component ray = (Component)adapter.GetMethod("Create").Invoke(null, new object[] { template, "B-RAY", pool, pilot, anchor.transform, new Vector3(0, 4, 44) });
            Component karden = (Component)adapter.GetMethod("Create").Invoke(null, new object[] { template, "B-KARDEN", pool, pilot, anchor.transform, new Vector3(24, 4, 44) });
            try
            {
                Type statusType = adapter.Assembly.GetType("ProjectG.FlightCraftedStatus");
                Component status = ray.gameObject.AddComponent(statusType);
                Vector3 before = ray.transform.position;
                Assert.That(statusType.GetMethod("TryKnockback", Hidden).Invoke(status, new object[] { Vector3.right, 2f }), Is.True);
                Assert.That(Vector3.Distance(ray.transform.position, before), Is.EqualTo(1f).Within(0.01f));
                Assert.That(statusType.GetMethod("TryRoot", Hidden).Invoke(status, new object[] { 1.2f }), Is.True);
                Assert.That((float)adapter.GetField("rootedUntil", Hidden).GetValue(ray) - Time.time, Is.EqualTo(0.6f).Within(0.01f));
                Component bossStatus = karden.gameObject.AddComponent(statusType);
                Assert.That(statusType.GetMethod("TryRoot", Hidden).Invoke(bossStatus, new object[] { 1.2f }), Is.False);
                Assert.That(statusType.GetMethod("TryKnockback", Hidden).Invoke(bossStatus, new object[] { Vector3.right, 2f }), Is.False);
                Component enemy = (Component)adapter.GetProperty("Enemy").GetValue(ray);
                adapter.GetMethod("StopCombat").Invoke(ray, null);
                Assert.That(enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(600));
                Assert.That(enemy.GetType().GetProperty("IsCombatTarget").GetValue(enemy), Is.False, "retreat need not fake a zero gauge to stop targeting");
            }
            finally { UnityEngine.Object.Destroy(ray.gameObject); UnityEngine.Object.Destroy(karden.gameObject); UnityEngine.Object.Destroy(anchor); }
        }

        [UnityTest]
        public IEnumerator FixedDebrisNeverFallsUntilTheAuthoredFallingWaveConfiguresIt()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/FlightPrototype.unity");
            yield return null;
            Type adapter = Type.GetType("ProjectG.FlightStoryCombatant, Assembly-CSharp", true);
            Component pilot = UnityEngine.Object.FindFirstObjectByType(adapter.Assembly.GetType("ProjectG.FlightPilotVitals")) as Component;
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false;
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false;
            Component template = GameObject.Find("GlassfinEnemy").GetComponent("FlightEnemy");
            Component pool = GameObject.Find("ThreatPool").GetComponent("FlightThreatPool");
            var anchor = new GameObject("StoryRockAnchor");
            anchor.transform.position = Vector3.up * 30f;
            Component rock = (Component)adapter.GetMethod("Create").Invoke(null, new object[] { template, "H-ROCK", pool, pilot, anchor.transform, new Vector3(0, 18, 44) });
            try
            {
                adapter.GetMethod("SetAttackEnabled").Invoke(rock, new object[] { true });
                Vector3 start = rock.transform.position;
                yield return new WaitForSeconds(2.2f);
                Assert.That(rock.transform.position, Is.EqualTo(start), "fixed initial debris has no fall/contact timer");
                adapter.GetMethod("ConfigureRock").Invoke(rock, new object[] { true, 8f });
                yield return new WaitForSeconds(1.8f);
                Assert.That(rock.transform.position, Is.EqualTo(start), "full two-second falling warning");
                yield return new WaitForSeconds(1.6f);
                Assert.That(rock.transform.position.y, Is.EqualTo(38f).Within(0.05f), "the authored upper landing height is preserved");
                Vector3 landed = rock.transform.position;
                yield return new WaitForSeconds(0.4f);
                Assert.That(rock.transform.position, Is.EqualTo(landed), "landed debris stays inert until cleared");
            }
            finally { UnityEngine.Object.Destroy(rock.gameObject); UnityEngine.Object.Destroy(anchor); }
        }

        private static object MakeShots(Type shotType, IEnumerable plans)
        {
            var values = new System.Collections.Generic.List<object>();
            foreach (object plan in plans)
            {
                object shot = Activator.CreateInstance(shotType);
                shotType.GetField("Position").SetValue(shot, new Vector3(25, 30, 0));
                shotType.GetField("Direction").SetValue(shot, Vector3.forward);
                shotType.GetField("Plan").SetValue(shot, plan);
                values.Add(shot);
            }
            Array array = Array.CreateInstance(shotType, values.Count);
            for (int i = 0; i < values.Count; i++) array.SetValue(values[i], i);
            return array;
        }

        private static Delegate MakeCallback(Type eventType, Action action)
        {
            Type argument = eventType.GetGenericArguments()[0];
            MethodInfo method = typeof(FlightStoryCombatPlayTests).GetMethod(nameof(Callback), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(argument);
            return (Delegate)method.Invoke(null, new object[] { action });
        }

        private static Action<T> Callback<T>(Action action) => _ => action();
    }
}
