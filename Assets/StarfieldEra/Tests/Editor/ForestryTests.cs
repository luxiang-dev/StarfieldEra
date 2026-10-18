using NUnit.Framework;
using UnityEngine;
namespace StarfieldEra.Tests
{
    public class ForestryTests
    {
        [Test] public void TreeCutIsPermanentAndCannotPayTwice()
        {
            var s=new ForestryState();var p=new Vector3(57.12f,3,39.45f);
            Assert.That(s.Fell(p),Is.True);Assert.That(s.Fell(p+Vector3.up*5),Is.False);
            var loaded=JsonUtility.FromJson<ForestryState>(JsonUtility.ToJson(s));
            Assert.That(loaded.Contains(p),Is.True);Assert.That(loaded.Valid(),Is.True);
        }
        [Test] public void DuplicateAndOutOfBoundsCutsAreRejected()
        {
            var s=new ForestryState();s.felled.Add(new Vector2Int(0,0));s.felled.Add(new Vector2Int(0,0));Assert.That(s.Valid(),Is.False);
            s.felled.Clear();s.felled.Add(new Vector2Int(int.MinValue,0));Assert.That(s.Valid(),Is.False);
        }
        [Test] public void LegacyFarmGetsEmptyForestryState()
        {
            var farm=new FarmState();farm.forestry=null;farm.Upgrade();Assert.That(farm.forestry.Valid(),Is.True);Assert.That(farm.forestry.felled,Is.Empty);Assert.That(farm.IsValid(),Is.True);
        }
    }
}
