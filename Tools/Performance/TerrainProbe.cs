// Unity CLI eval_file snippet, intentionally outside Assets (not a compiled game script).
// Run in Play mode with the same save and terrain before/after an optimization.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play mode and wait for world initialization first.");
var surface=UnityEngine.Object.FindFirstObjectByType<StarfieldEra.ContinuousTerrain>();
if(!surface)throw new System.InvalidOperationException("World has not initialized.");
var grid=surface.GetComponent<StarfieldEra.HexWorldGrid>();
var points=new UnityEngine.Vector3[1000];
for(int i=0;i<points.Length;i++)points[i]=new UnityEngine.Vector3(-70+(i%40)*3.5f,0,-60+(i/40)*5f);
float checksum=0;
foreach(var p in points)checksum+=surface.HeightAt(p);
var watch=new System.Diagnostics.Stopwatch();
watch.Start();for(int pass=0;pass<3;pass++)foreach(var p in points)checksum+=surface.HeightAt(p);watch.Stop();
string report="Height3000 ms="+watch.Elapsed.TotalMilliseconds;
watch.Restart();foreach(var p in points)checksum+=grid.CanStand(p)?1:0;watch.Stop();
report+=" CanStand1000 ms="+watch.Elapsed.TotalMilliseconds+" checksum="+checksum;
string folder=System.IO.Path.GetFullPath("Temp/TerrainPerformance");System.IO.Directory.CreateDirectory(folder);
System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"queries.txt"),report);
// Deferred to let the CLI return before collider cooking. Read rebuild.txt after completion.
UnityEditor.EditorApplication.delayCall+=()=>{
    var timer=System.Diagnostics.Stopwatch.StartNew();surface.RebuildSurface();timer.Stop();
    string meshes="";
    using(var stream=System.IO.File.Create(System.IO.Path.Combine(folder,"surface.bin")))
    using(var writer=new System.IO.BinaryWriter(stream)){
        for(int z=-70;z<=70;z++)for(int x=-82;x<=82;x++){
            var p=new UnityEngine.Vector3(x+.13f,0,z+.21f);writer.Write(surface.HeightAt(p));writer.Write(surface.SupportsWalking(p));
        }
        foreach(var filter in surface.GetComponentsInChildren<UnityEngine.MeshFilter>())
            if(filter.name=="Continuous land and lakebed"||filter.name=="Animated translucent water"){
                var mesh=filter.sharedMesh;meshes+=" "+filter.name+":"+mesh.vertexCount+"/"+mesh.triangles.Length;
                foreach(var v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                foreach(var c in mesh.colors){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                foreach(var index in mesh.triangles)writer.Write(index);
            }
    }
    System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"rebuild.txt"),"Rebuild ms="+timer.Elapsed.TotalMilliseconds+meshes);
};
return report+"; deferred results: "+folder;
