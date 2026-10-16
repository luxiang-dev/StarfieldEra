using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace StarfieldEra
{
    /// <summary>A stable target, not a per-frame LookAt at a bouncing character capsule.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class PlayerCameraRig:MonoBehaviour
    {
        FarmGame game;Camera view;Vector3 pivot,velocity;float yaw=-35,pitch=43,zoom=10,targetZoom=10,distance=8;
        float targetYaw=-35,targetPitch=43,verticalVelocity;bool ready;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static float Safe(float value,float fallback)=>Finite(value)?value:fallback;
        static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
        public int Mode {get;private set;}
        public bool IsFirstPerson=>Mode==3;
        readonly Dictionary<Renderer,ShadowCastingMode> bodyShadows=new Dictionary<Renderer,ShadowCastingMode>();
        public static readonly string[] ModeNames={"地图视角","高空视角","近景视角","第一人称"};
        public void Initialize(FarmGame owner)
        {
            game=owner;view=GetComponent<Camera>();view.farClipPlane=180;
            Mode=Mathf.Clamp(PlayerPrefs.GetInt("StarfieldEra.CameraMode",0),0,3);
            targetZoom=Mathf.Clamp(Safe(PlayerPrefs.GetFloat("StarfieldEra.CameraZoom",10),10),5,26);
            targetYaw=Safe(PlayerPrefs.GetFloat("StarfieldEra.CameraYaw",-35),-35);ApplyMode();Save();
            pivot=game.World.Player.position+Vector3.up*.85f;yaw=targetYaw;pitch=targetPitch;zoom=targetZoom;ready=true;Place(true);
        }
        void ApplyMode(){targetPitch=Mode==0?43:Mode==1?78:Mode==2?24:0;UpdateBodyVisibility();}
        void UpdateBodyVisibility()
        {
            if(!game||!game.World)return;
            if(IsFirstPerson)
            {foreach(var r in game.World.Player.GetComponentsInChildren<Renderer>())if(!bodyShadows.ContainsKey(r)){bodyShadows[r]=r.shadowCastingMode;r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;}}
            else RestoreBody();
        }
        void RestoreBody(){foreach(var item in bodyShadows)if(item.Key)item.Key.shadowCastingMode=item.Value;bodyShadows.Clear();}
        void OnDisable(){RestoreBody();}
        void OnEnable(){if(ready)UpdateBodyVisibility();}
        public void SetMode(int mode){Mode=Mathf.Clamp(mode,0,3);ApplyMode();pitch=targetPitch;Save();if(ready)Place(true);}
        public void Turn(float degrees){if(IsFirstPerson){game.World.Player.Rotate(0,degrees,0);return;}targetYaw=Mathf.Repeat(Safe(targetYaw,-35)+Mathf.Clamp(Safe(degrees,0),-180,180)+180,360)-180;Save();}
        public void AdjustZoom(float amount){targetZoom=Mathf.Clamp(Safe(targetZoom,10)+Safe(amount,0),5,26);Save();}
        public void ResetView(){targetYaw=-35;targetZoom=10;ApplyMode();velocity=Vector3.zero;verticalVelocity=0;Save();}
        void Save(){PlayerPrefs.SetInt("StarfieldEra.CameraMode",Mode);PlayerPrefs.SetFloat("StarfieldEra.CameraZoom",targetZoom);PlayerPrefs.SetFloat("StarfieldEra.CameraYaw",targetYaw);}
        void LateUpdate()
        {
            if(!ready||!game||Time.unscaledDeltaTime<=0)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            targetYaw=Safe(targetYaw,-35);targetPitch=Mathf.Clamp(Safe(targetPitch,43),IsFirstPerson?-75:12,IsFirstPerson?75:78);targetZoom=Mathf.Clamp(Safe(targetZoom,10),5,26);
            yaw=Safe(yaw,targetYaw);pitch=Safe(pitch,targetPitch);zoom=Safe(zoom,targetZoom);
            bool inputAllowed=!(game.Management&&game.Management.IsOpen)&&!game.Adventure.InBattle&&!game.Expedition.Fishing;
            if(inputAllowed)
            {
                var keyboard=Keyboard.current;var mouse=Mouse.current;
                if(keyboard!=null&&keyboard.vKey.wasPressedThisFrame)SetMode((Mode+1)%ModeNames.Length);
                if(keyboard!=null&&keyboard.homeKey.wasPressedThisFrame)ResetView();
                if(mouse!=null&&!(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))
                {
                    float scroll=Safe(mouse.scroll.ReadValue().y,0);if(Mathf.Abs(scroll)>.01f)AdjustZoom(-scroll*.008f);
                    if(mouse.middleButton.isPressed)
                    {
                        var delta=mouse.delta.ReadValue();
                        if(IsFirstPerson){game.World.Player.Rotate(0,Mathf.Clamp(Safe(delta.x,0),-400,400)*.18f,0);targetPitch=Mathf.Clamp(targetPitch-Mathf.Clamp(Safe(delta.y,0),-400,400)*.12f,-75,75);}
                        else {targetYaw=Mathf.Repeat(targetYaw+Mathf.Clamp(Safe(delta.x,0),-400,400)*.18f+180,360)-180;
                        if(Mode!=1)targetPitch=Mathf.Clamp(targetPitch-Mathf.Clamp(Safe(delta.y,0),-400,400)*.12f,Mode==0?28:12,Mode==0?65:55);}
                    }
                    if(mouse.middleButton.wasReleasedThisFrame)Save();
                }
            }
            if(IsFirstPerson){pitch=Mathf.Lerp(pitch,targetPitch,1-Mathf.Exp(-15*dt));Place(false);return;}
            var target=game.World.Player.position+Vector3.up*.85f;
            if(!Finite(target))return;
            if(!Finite(pivot)||!Finite(velocity)||!Finite(verticalVelocity)){pivot=target;velocity=Vector3.zero;verticalVelocity=0;}
            if((new Vector2(target.x-pivot.x,target.z-pivot.z)).sqrMagnitude>100){pivot=target;velocity=Vector3.zero;verticalVelocity=0;}
            // Horizontal damping remains quick; height uses a dead zone and slower response.
            var horizontal=Vector3.SmoothDamp(new Vector3(pivot.x,0,pivot.z),new Vector3(target.x,0,target.z),ref velocity,.13f,Mathf.Infinity,dt);
            pivot.x=horizontal.x;pivot.z=horizontal.z;
            float dy=target.y-pivot.y;float heightTarget=Mathf.Abs(dy)<.12f?pivot.y:target.y-Mathf.Sign(dy)*.12f;
            pivot.y=Mathf.SmoothDamp(pivot.y,heightTarget,ref verticalVelocity,.48f,Mathf.Infinity,dt);
            float blend=1-Mathf.Exp(-8*dt);
            yaw=Mathf.LerpAngle(yaw,targetYaw,blend);pitch=Mathf.Lerp(pitch,targetPitch,blend);zoom=Mathf.Lerp(zoom,targetZoom,blend);
            Place(false);
        }
        void Place(bool snap)
        {
            if(IsFirstPerson)
            {
                view.orthographic=false;view.fieldOfView=72;view.nearClipPlane=.04f;
                view.transform.SetPositionAndRotation(game.World.Player.position+Vector3.up*1.62f,Quaternion.Euler(pitch,game.World.Player.eulerAngles.y,0));return;
            }
            view.nearClipPlane=.15f;
            var rotation=Quaternion.Euler(pitch,yaw,0);view.orthographic=Mode!=2;
            view.orthographicSize=zoom;view.fieldOfView=48;
            float desired=Mode==2?Mathf.Lerp(3.8f,15,Mathf.InverseLerp(5,26,zoom)):34;
            if(Mode==2)
            {
                var backward=-(rotation*Vector3.forward);
                // Retract from solid scenery without reacting to our own character or labels.
                foreach(var hit in Physics.SphereCastAll(pivot,.22f,backward,desired,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.transform.IsChildOf(game.World.Player)||hit.collider is CharacterController)continue;
                    desired=Mathf.Min(desired,Mathf.Max(.8f,hit.distance-.18f));
                }
            }
            distance=snap?desired:Mathf.Lerp(distance,desired,1-Mathf.Exp(-10*Time.unscaledDeltaTime));
            // Ground clearance prevents the low view going through a hillside with no physics collider nearby.
            var position=pivot-rotation*Vector3.forward*distance;
            if(Mode==2&&game.World.HexGrid)position.y=Mathf.Max(position.y,game.World.HexGrid.GroundHeight(position)+.45f);
            view.transform.SetPositionAndRotation(position,rotation);
        }
    }
}
