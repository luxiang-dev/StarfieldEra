using UnityEngine;

namespace StarfieldEra
{
    /// <summary>Local precipitation and smoothly blended atmosphere; owns and restores its resources.</summary>
    public sealed class WeatherSystem : MonoBehaviour
    {
        FarmGame game;
        Light sun;
        Color clearSun, clearAmbient, clearSky, previousFogColor;
        float clearIntensity, clearShadow, previousFogDensity;
        bool previousFog;
        FogMode previousFogMode;
        ParticleSystem rain, snow;
        Material precipitationMaterial;
        float cloud, fog, rainAmount, snowAmount,stormClock,flash;
        static readonly int FogProperty = Shader.PropertyToID("_StarfieldWeatherFog");
        Vector4 previousShaderFog;
        public WeatherType Current => game.State.Weather;

        public void Initialize(FarmGame owner)
        {
            game = owner;
            foreach (var light in game.World.GetComponentsInChildren<Light>())
                if (light.type == LightType.Directional) { sun = light; break; }
            clearSun = sun ? sun.color : Color.white;
            clearIntensity = sun ? sun.intensity : 1;
            clearShadow = sun ? sun.shadowStrength : .8f;
            clearAmbient = RenderSettings.ambientLight;
            clearSky = game.World.View.backgroundColor;
            previousFog = RenderSettings.fog;
            previousFogMode = RenderSettings.fogMode;
            previousFogDensity = RenderSettings.fogDensity;
            previousFogColor = RenderSettings.fogColor;
            previousShaderFog = Shader.GetGlobalVector(FogProperty);
            precipitationMaterial = new Material(Resources.Load<Shader>("WeatherParticles"));
            rain = CreatePrecipitation("Rain around player", false);
            snow = CreatePrecipitation("Snow around player", true);
            Apply(true);
        }

        ParticleSystem CreatePrecipitation(string name, bool flakes)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            var system = root.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false; main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = flakes ? 650 : 1500;
            main.startLifetime = flakes ? 7 : 1.4f;
            main.startSpeed = 0;
            main.startSize = flakes ? .085f : .035f;
            main.startColor = flakes ? new Color(.93f,.97f,1,.85f) : new Color(.65f,.8f,.9f,.55f);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(38,.2f,38);
            var velocity = system.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = flakes ? .35f : -1.4f;
            velocity.y = flakes ? -1.9f : -18;
            velocity.z = flakes ? .15f : -.6f;
            var emission = system.emission; emission.rateOverTime = 0;
            var collision = system.collision; collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.collidesWith = Physics.DefaultRaycastLayers;
            collision.lifetimeLoss = 1; collision.maxCollisionShapes = 64;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = precipitationMaterial;
            renderer.renderMode = flakes ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 7; renderer.velocityScale = .025f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        void LateUpdate() { if (game) Apply(false); }

        void Apply(bool instant)
        {
            Shader.SetGlobalFloat("_StarfieldNight",game.Night&&game.Night.Active?1:0);
            var kind = Current;
            float blend = instant ? 1 : 1 - Mathf.Exp(-Time.deltaTime / 2);
            float cloudTarget = kind == WeatherType.Sunny ? 0 : kind == WeatherType.Cloudy ? .45f : kind==WeatherType.LightRain?.65f:1;
            cloud = Mathf.Lerp(cloud, cloudTarget, blend);
            fog = Mathf.Lerp(fog, kind==WeatherType.HeavyRain?.22f:kind==WeatherType.Thunderstorm?.35f:0, blend);
            rainAmount = Mathf.Lerp(rainAmount, kind==WeatherType.LightRain?.32f:kind==WeatherType.HeavyRain?.78f:kind==WeatherType.Thunderstorm?1:0, blend);
            snowAmount = Mathf.Lerp(snowAmount, 0, blend);
            stormClock+=Time.deltaTime;flash=Mathf.MoveTowards(flash,0,Time.deltaTime*7);
            if(kind==WeatherType.Thunderstorm&&stormClock>=7.5f){stormClock=0;flash=.65f;}else if(kind!=WeatherType.Thunderstorm){flash=0;stormClock=0;}
            if (sun)
            {
                sun.color = Color.Lerp(clearSun, new Color(.76f,.84f,.94f), cloud * .6f);
                sun.intensity = clearIntensity * (Mathf.Lerp(1,.40f,cloud)*(game.Night&&game.Night.Active?.3f:1)+flash);
                sun.shadowStrength = clearShadow * Mathf.Lerp(1,.35f,cloud);
            }
            RenderSettings.ambientLight = Color.Lerp(clearAmbient, new Color(.40f,.47f,.53f), cloud * .7f);
            if(game.Night&&game.Night.Active)RenderSettings.ambientLight=Color.Lerp(RenderSettings.ambientLight,new Color(.18f,.22f,.34f),.65f);
            var sky = game.Night&&game.Night.Active?new Color(.08f,.12f,.23f):Color.Lerp(clearSky, new Color(.48f,.57f,.65f), cloud);
            game.World.View.backgroundColor = sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(sky, new Color(.72f,.78f,.77f), fog);
            RenderSettings.fogDensity = .002f + cloud * .004f + fog * .025f;
            Color linearFog = RenderSettings.fogColor.linear;
            Shader.SetGlobalVector(FogProperty, new Vector4(linearFog.r, linearFog.g, linearFog.b, RenderSettings.fogDensity));
            // Cover the viewed area, including when the player changes camera direction or fast travels.
            Vector3 center = game.World.Player.position;
            Vector3 viewOffset = game.World.View.transform.position - center;
            viewOffset.y = 0;
            center += Vector3.ClampMagnitude(viewOffset * .35f, 7);
            Vector3 position = center + Vector3.up * 12;
            if ((rain.transform.position-position).sqrMagnitude > 45*45)
            { rain.Clear(); snow.Clear(); }
            rain.transform.position = snow.transform.position = position;
            SetEmission(rain, rainAmount * 900);
            SetEmission(snow, snowAmount * 80);
        }

        static void SetEmission(ParticleSystem system, float rate)
        {
            var emission = system.emission; emission.rateOverTime = rate;
            if (rate > .5f && !system.isPlaying) system.Play();
            if (rate <= .5f && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void OnDestroy()
        {
            if (precipitationMaterial) Destroy(precipitationMaterial);
            if (ReferenceEquals(game, null)) return;
            RenderSettings.fog = previousFog;
            RenderSettings.fogMode = previousFogMode;
            RenderSettings.fogDensity = previousFogDensity;
            RenderSettings.fogColor = previousFogColor;
            Shader.SetGlobalVector(FogProperty, previousShaderFog);
            Shader.SetGlobalFloat("_StarfieldNight",0);
            RenderSettings.ambientLight = clearAmbient;
            if (sun) { sun.color = clearSun; sun.intensity = clearIntensity; sun.shadowStrength = clearShadow; }
            if (game.World && game.World.View) game.World.View.backgroundColor = clearSky;
        }
    }
}
