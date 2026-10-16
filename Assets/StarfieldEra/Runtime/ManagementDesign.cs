using System;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class ManagementHud
    {
        string PageIntroduction()
        {
            switch(page)
            {
                case "production":return "资源点经营 · 分配村民、管理库存与升级设施";
                case "buildings":return "建筑管理 · 查看建筑，调整位置或拆除返还";
                case "camp":return "营地建设 · 修建防御，准备夜间守卫";
                case "building":return "建筑经营 · 管理村民、库存与升级，调整建筑位置与朝向";
                case "inventory":return "随身行囊 · 整理材料、种子与装备，准备下一段旅途";
                case "weather":return "观天识候 · 掌握今日变化，用经验预判未来";
                case "tasks":return "旅途委托 · 追踪目标、查看奖励与当前进度";
                case "craft":return "匠人工作台 · 学习图纸，制作装备与远行补给";
                case "town":return "聚落规划 · 自由建设、研究科技与管理建筑";
                case "companions":return "同行伙伴 · 了解能力，选择陪你远行的伙伴";
                case "map":return "大陆舆图 · 探索未知区域，规划下一处目的地";
                case "market":return "商旅集市 · 挑选种子、出售资源与完成订单";
                case "land":return "土地与生态 · 开垦、育林、灌溉和整理地形";
                case "tutorial":return "旅人指南 · 查阅操作、农耕、建造与冒险玩法";
                case "camera":return "观察世界 · 选择视角，调整方向和距离";
                default:return "营地生活 · 照料兔子、经营资源点与准备夜间守卫";
            }
        }
        void DetailIcon(Transform parent,string title)
        {
            int building=Array.IndexOf(HexConstruction.Names,title);
            if(building>=0){UITheme.Icon(parent,building,20,14,66,66,false,true);return;}
            if(page=="inventory"){UITheme.Icon(parent,UITheme.ItemIcon(itemKey),20,14,66,66,true);return;}
            if(page=="craft")
            {string key=equipmentView?EquipmentCatalog.Recipes[equipmentSelection].key:RecipeKeys[selectedRecipe];UITheme.Icon(parent,UITheme.ItemIcon(key),20,14,66,66,true);return;}
            UITheme.Icon(parent,UITheme.PageIcon(page),20,14,66,66);
        }
        void RowIcon(Transform parent,string title,string description,float x,float y,float size)
        {
            int building=Array.IndexOf(HexConstruction.Names,title);
            if(building>=0){UITheme.Icon(parent,building,x,y,size,size,false,true);return;}
            var equipment=EquipmentCatalog.Recipes.FirstOrDefault(r=>r.name==title);
            if(equipment!=null){UITheme.Icon(parent,UITheme.ItemIcon(equipment.key),x,y,size,size,true);return;}
            int recipe=Array.IndexOf(RecipeNames,title);
            if(recipe>=0){UITheme.Icon(parent,UITheme.ItemIcon(RecipeKeys[recipe]),x,y,size,size,true);return;}
            if(page=="market")
            {int icon=title.Contains("种子")?12+Mathf.Clamp(Array.IndexOf(FarmState.CropNames,title.Replace("种子","")),0,2):title.Contains("木材")?0:title.Contains("鲜鱼")?6:29;UITheme.Icon(parent,icon,x,y,size,size,true);return;}
            if(page=="homestead")
            {int icon=title.Contains("兔")?23:title.Contains("陷阱")?22:title.Contains("林场")?18:title.Contains("采石")?19:11;UITheme.Icon(parent,icon,x,y,size,size);return;}
            UITheme.Icon(parent,UITheme.PageIcon(page),x,y,size,size);
        }
    }
}
