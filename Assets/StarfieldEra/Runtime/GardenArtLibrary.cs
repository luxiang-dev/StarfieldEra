using UnityEngine;
namespace StarfieldEra
{
    public sealed class GardenArtLibrary:ScriptableObject
    {
        public GameObject peashooter,wall,tomb,zombie,twinPea,icePea,coneZombie,bucketZombie,bruteZombie,chomper,lantern;
        public AnimationClip walk,dismay;
        public AnimationClip[] variantWalk=new AnimationClip[3],variantDismay=new AnimationClip[3];
        public AnimationClip Walk(int variant)=>variant>0&&variant<=variantWalk.Length?variantWalk[variant-1]:walk;
        public AnimationClip Dismay(int variant)=>variant>0&&variant<=variantDismay.Length?variantDismay[variant-1]:dismay;
        public GameObject Unit(int kind)=>kind==0?peashooter:kind==1?wall:kind==2?tomb:kind==3?twinPea:kind==4?icePea:kind==5?chomper:kind==6?lantern:null;
        public GameObject Zombie(int variant)=>variant==1?coneZombie:variant==2?bucketZombie:variant==3?bruteZombie:zombie;
    }
}
