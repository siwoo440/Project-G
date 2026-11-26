using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectG.Tests
{
    public sealed class FlightStoryMissionTests
    {
        private static Type Runtime(string name)
        {
            Type type = Type.GetType("ProjectG." + name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "The game must use the production story runtime: " + name);
            return type;
        }

        private static object Create(string name, params object[] args) => Activator.CreateInstance(Runtime(name), args);
        private static object EnumValue(string name, string value) => Enum.Parse(Runtime(name), value);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static void Field(object target, string name, object value) => target.GetType().GetField(name).SetValue(target, value);
        private static object Call(object target, string name, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(name);
            ParameterInfo[] parameters = method.GetParameters();
            var complete = new object[parameters.Length];
            for (int i = 0; i < complete.Length; i++) complete[i] = i < args.Length ? args[i] : parameters[i].DefaultValue;
            return method.Invoke(target, complete);
        }

        [Test]
        public void SceneProgressRejectsStaleAndUnregisteredDialogueEvidence()
        {
            object attempt = Create("FlightStoryAttempt", "current", "ST-X", new[] { "A" }, EnumValue("FlightStoryPlayMode", "Story"));
            object required = Create("FlightStorySceneRequirements");
            Field(required, "Facts", new[] { "actual-rescue" });
            Assert.That(Call(attempt, "DefineScene", "A", required), Is.True);
            Assert.That(Call(attempt, "SetFact", "old", "A", "actual-rescue", true), Is.False);
            Assert.That(Call(attempt, "SetFact", "current", "A", "dialogue-skipped", true), Is.False);
            Assert.That(Call(attempt, "TryAdvance"), Is.False);
            Assert.That(Call(attempt, "SetFact", "current", "A", "actual-rescue", true), Is.True);
            Assert.That(Call(attempt, "TryAdvance"), Is.True);
            Assert.That(Property(attempt, "Status").ToString(), Is.EqualTo("Success"));
        }

        [Test]
        public void EmptyRosterAndWrongDestinationCannotSatisfyEvacuation()
        {
            Array empty = Array.CreateInstance(Runtime("FlightStoryManifestEntry"), 0);
            object invalid = Create("FlightStoryManifest", "current", empty);
            Assert.That(Call(invalid, "Seal"), Is.False);
            Assert.That(Property(invalid, "AllArrived"), Is.False);
            object person = Create("FlightStoryManifestEntry");
            Field(person, "Id", "worker-1"); Field(person, "GroupId", "operations"); Field(person, "DestinationId", "safe-platform");
            Assert.That(Call(invalid, "AddDiscovered", person), Is.True);
            Assert.That(Call(invalid, "Seal"), Is.True);
            Assert.That(Call(invalid, "ConfirmArrival", "current", "worker-1", "safe-platform"), Is.False);
            Assert.That(Call(invalid, "ConfirmDeparture", "current", "worker-1"), Is.True);
            Assert.That(Call(invalid, "ConfirmArrival", "current", "worker-1", "wrong-platform"), Is.False);
            Assert.That(Call(invalid, "ConfirmArrival", "current", "worker-1", "safe-platform"), Is.True);
            Assert.That(Property(invalid, "AllArrived"), Is.True);
        }

        [Test]
        public void NonlethalBossNeedsSafetyAndKeepsExactlyOneRewardClassification()
        {
            object attempt = Create("FlightStoryAttempt", "current", "ST-2-2", new[] { "C02-S02", "C02-S03" }, EnumValue("FlightStoryPlayMode", "Story"));
            Assert.That(Call(attempt, "BeginCombat", "current"), Is.True);
            Assert.That(Call(attempt, "RegisterEncounter", "karden", EnumValue("FlightStoryRewardClass", "Mid"), true), Is.True);
            Assert.That(Call(attempt, "RegisterEncounter", "karden", EnumValue("FlightStoryRewardClass", "Final"), true), Is.False);
            Assert.That(Call(attempt, "TryResolveEncounter", "current", "karden", false), Is.False);
            Assert.That(Call(attempt, "TryResolveEncounter", "current", "karden", true), Is.True);
            Assert.That(Call(attempt, "TryResolveEncounter", "current", "karden", true), Is.False);
            Assert.That(Property(attempt, "MidCount"), Is.EqualTo(1));
            Assert.That(Property(attempt, "FinalCount"), Is.Zero);
        }

        [Test]
        public void IndividualProtectionCannotHealOrTransferItsLossToAnotherTarget()
        {
            object ship = Create("FlightStoryProtectedTarget", "current", "ship", 240d);
            object platform = Create("FlightStoryProtectedTarget", "current", "platform", 240d);
            Assert.That(Call(ship, "TryHit", "current", "shot-1", 239d, 0d), Is.True);
            Assert.That(Property(ship, "Durability"), Is.EqualTo(1d));
            Assert.That(Call(ship, "TryHit", "current", "shot-1", 239d, 2d), Is.False);
            Assert.That(Call(ship, "TryHit", "current", "shot-2", 1d, 2d), Is.True);
            Assert.That(Property(ship, "IsFailed"), Is.True);
            Assert.That(Property(platform, "Durability"), Is.EqualTo(240d));
        }

        [Test]
        public void CumulativeWorkAndContinuousHoldHaveDifferentRecovery()
        {
            object work = Create("FlightStoryProgress", 60d, false);
            object hold = Create("FlightStoryProgress", 5d, true);
            Call(work, "Step", 20d, true); Call(hold, "Step", 4d, true);
            Call(work, "Step", 1d, false); Call(hold, "Step", 1d, false);
            Assert.That(Property(work, "Seconds"), Is.EqualTo(20d));
            Assert.That(Property(hold, "Seconds"), Is.EqualTo(0d));
            Call(work, "Step", 100d, true, true);
            Assert.That(Property(work, "Seconds"), Is.EqualTo(20d));
        }

        [Test]
        public void EvidenceComponentDoesNotCompleteAnythingOnEnableOrWithoutBinding()
        {
            var gameObject = new GameObject("Story evidence test");
            try
            {
                Component signal = gameObject.AddComponent(Runtime("FlightStoryEvidenceSignal"));
                Call(signal, "Configure", "C08-S07", "commander_approved");
                gameObject.SetActive(false); gameObject.SetActive(true);
                Assert.That(Call(signal, "SetPresent", "unbound-run", true), Is.False);
                Assert.That(Call(signal, "Bind", new object[] { null }), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); }
        }

        [Test]
        public void DuplicateRosterIdentityIsAnAuthoringError()
        {
            var root = new GameObject("Story roster test");
            try
            {
                var first = new GameObject("First display name"); first.transform.SetParent(root.transform);
                var second = new GameObject("Different display name"); second.transform.SetParent(root.transform);
                Call(first.AddComponent(Runtime("FlightStoryRosterMember")), "Configure", "same-stable-id");
                Call(second.AddComponent(Runtime("FlightStoryRosterMember")), "Configure", "same-stable-id");
                var errors = (string[])Runtime("FlightStorySignals").GetMethod("ValidateHierarchy").Invoke(null, new object[] { root.transform });
                Assert.That(errors.Length, Is.GreaterThan(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ArrivalGateNeedsABoundExplicitRosterIdentity()
        {
            var root = new GameObject("Story arrival test");
            try
            {
                Component gate = root.AddComponent(Runtime("FlightStoryArrivalGate"));
                Call(gate, "Configure", "safe-platform");
                Component member = root.AddComponent(Runtime("FlightStoryRosterMember"));
                Call(member, "Configure", "worker-1");
                Assert.That(Call(gate, "TryArrive", member), Is.False);
                Assert.That(Call(gate, "TryArrive", new object[] { null }), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
