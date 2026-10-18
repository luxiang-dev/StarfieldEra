using System;
using System.IO;
using UnityEngine;
namespace StarfieldEra
{
    public static class SaveSlots
    {
        public const int Count=3;
        public static int Active {get=>Mathf.Clamp(PlayerPrefs.GetInt("Farm.ActiveSlot",0),0,Count-1);set{PlayerPrefs.SetInt("Farm.ActiveSlot",Mathf.Clamp(value,0,Count-1));PlayerPrefs.Save();}}
        public static string PathFor(int slot)=>Path.Combine(Application.persistentDataPath,slot==0?"farm-v1.json":"farm-slot-"+slot+".json");
        public static bool Exists(int slot)=>File.Exists(PathFor(slot))||File.Exists(PathFor(slot)+".bak");
        public static string Description(int slot)
        {
            if(!Exists(slot))return "空存档 · 开始新的旅程";
            return FarmSave.TryLoad(PathFor(slot),out var s)?"第 "+s.day+" 天 · 金币 "+s.coins:"存档无法读取 · 可重新开始";
        }
        // Preserve both generations before replacing a world. Never overwrite the legacy slot on migration.
        public static void Restart(int slot)
        {
            string path=PathFor(slot), archive=Path.Combine(Application.persistentDataPath,"Archives",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            Directory.CreateDirectory(archive);
            foreach(string suffix in new[]{"",".bak"})if(File.Exists(path+suffix))File.Copy(path+suffix,Path.Combine(archive,Path.GetFileName(path+suffix)));
            var fresh=new FarmState();fresh.Upgrade();FarmSave.Save(path,fresh);
            // The restart backup must also belong to the new world, not the retired world.
            File.Copy(path,path+".bak",true);
        }
    }
}
