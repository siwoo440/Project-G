using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FlightStoryIntegrationTests
{
    [Test]
    public void SafeSphereRejectsAabbCornerAndBodyOverlap()
    {
        var go = new GameObject("ReviewedExit");
        try
        {
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 5;
            MethodInfo method = Type.GetType("ProjectG.FlightStorySpatial, Assembly-CSharp", true).GetMethod("ContainsSphere");
            Assert.That((bool)method.Invoke(null, new object[] { sphere, new Vector3(4.5f, 4.5f, 0), 0f }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { sphere, new Vector3(4.5f, 0, 0), 1f }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { sphere, new Vector3(3, 0, 0), 1f }), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void SafeRotatedBoxUsesItsOrientedVolume()
    {
        var go = new GameObject("ReviewedRotatedExit");
        try
        {
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(2, 4, 12);
            go.transform.rotation = Quaternion.Euler(0, 45, 0);
            MethodInfo method = Type.GetType("ProjectG.FlightStorySpatial, Assembly-CSharp", true).GetMethod("ContainsSphere");
            Assert.That((bool)method.Invoke(null, new object[] { box, new Vector3(4, 0, -4), 0.1f }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { box, Vector3.zero, 0.5f }), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void DelayedEvidenceFromAnEarlierAttemptCannotCompleteTheNewAttempt()
    {
        var go = new GameObject("AttemptEvidenceTest");
        try
        {
            Type directorType = Type.GetType("ProjectG.FlightStoryDirector, Assembly-CSharp", true);
            Type signalType = Type.GetType("ProjectG.FlightStoryEvidenceSignal, Assembly-CSharp", true);
            Type attemptType = Type.GetType("ProjectG.FlightStoryAttempt, Assembly-CSharp", true);
            Type modeType = Type.GetType("ProjectG.FlightStoryPlayMode, Assembly-CSharp", true);
            Type requirementType = Type.GetType("ProjectG.FlightStorySceneRequirements, Assembly-CSharp", true);
            Component director = go.AddComponent(directorType);
            Component signal = go.AddComponent(signalType);
            signalType.GetMethod("Configure").Invoke(signal, new object[] { "P-S01", "observed" });
            Func<string, object> makeAttempt = run =>
            {
                object attempt = Activator.CreateInstance(attemptType, run, "ST-P-1", new[] { "P-S01" }, Enum.ToObject(modeType, 0));
                object requirements = Activator.CreateInstance(requirementType);
                requirementType.GetField("Facts").SetValue(requirements, new[] { "observed" });
                attemptType.GetMethod("DefineScene").Invoke(attempt, new[] { (object)"P-S01", requirements });
                return attempt;
            };
            object oldAttempt = makeAttempt("old-run");
            directorType.GetProperty("Attempt").SetValue(director, oldAttempt);
            directorType.GetProperty("Ready").SetValue(director, true);
            signalType.GetMethod("Bind").Invoke(signal, new object[] { director });
            object nextAttempt = makeAttempt("new-run");
            directorType.GetProperty("Attempt").SetValue(director, nextAttempt);
            signalType.GetMethod("Bind").Invoke(signal, new object[] { director });
            MethodInfo publish = signalType.GetMethod("SetPresent");
            Assert.That((bool)publish.Invoke(signal, new object[] { "old-run", true }), Is.False);
            Assert.That((bool)attemptType.GetMethod("HasFact").Invoke(nextAttempt, new object[] { "P-S01", "observed" }), Is.False);
            Assert.That((bool)publish.Invoke(signal, new object[] { "new-run", true }), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
