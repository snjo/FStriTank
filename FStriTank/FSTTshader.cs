using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace FStriTank
{
    class FSTTshader : PartModule
    {
        [KSPField]
        public string objectName = "flask";
        [KSPField]
        public int repeat = 0;
        [KSPField]
        public Vector4 color = new Vector4(0.1f, 0.1f, 1f, 1f);

        public static string AppPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        public static string PlugInDataPath = AppPath + "GameData/FStriTank/";
        public string shaderFile = PlugInDataPath + "FSFresnel.shader";

        private Transform targetObject;

        private static Material customShader;

        //private static Material customShader = new Material(
        //            "Shader \"Firespitter/FresnelAlpha\" {" +
        //            "Properties { " +
        //            "  _Color (\"Color\", Color) = (1.0, 1.0, 1.0, 1.0)      " +
        //            "  _RimPower (\"Rim Power\", Range(0.5,8.0)) = 3.0" +
        //            "  _Albedo (\"Gloss\", Range (0.01, 1)) = 0.5" +
        //            "}  " +
        //            "SubShader {  " +
        //            "  Tags { \"RenderType\" = \"Opaque\" }  " +
        //            "  CGPROGRAM  " +
        //            "  #pragma surface surf BlinnPhong alpha                    " +
        //            "  uniform float4 _Color;" +
        //            "  float _Albedo;      " +
        //            "  struct Input {  " +
        //            "      float2 uv_MainTex;  " +
        //            "      float3 worldRefl;" +
        //            "      float3 viewDir;" +
        //            "  };  " +
        //            "  sampler2D _MainTex;        " +
        //            "  float _RimPower;" +
        //            "  void surf (Input IN, inout SurfaceOutput o) {  " +
        //            "      float4 color = _Color;          " +
        //            "      half rim = saturate(dot (normalize(IN.viewDir), o.Normal));" +
        //            "      o.Emission = color.rgb * pow(rim,_RimPower);" +
        //            "      o.Alpha = color.a;          " +
        //            "      o.Albedo = color.rgb * _Albedo;" +
        //            "  }  " +
        //            "  ENDCG  " +
        //            "}   " +
        //            "Fallback \"Diffuse\"  " +
        //        "}"
        //    );

        public override void OnStart(PartModule.StartState state)
        {
            if (customShader == null)
            {
                Debug.Log("FSTTshader: reading shader " + shaderFile);
                StreamReader reader = new StreamReader(shaderFile);
                string shader = reader.ReadToEnd();
                Debug.Log("FSTTshader: shader length " + shader.Length);
                customShader = new Material(shader);            
            }

            if (repeat < 1)
            {
                targetObject = part.FindModelTransform(objectName);

                if (targetObject != null)
                {
                    setShader(targetObject);
                }
                else
                {
                    Debug.Log("FSTTshader: could not find object " + objectName);
                }
            }
            else
            {
                for (int i = 1; i <= repeat; i++)
                {
                    targetObject = part.FindModelTransform(objectName + repeat);
                    if (targetObject != null)
                    {
                        setShader(targetObject);
                    }
                    else
                    {
                        Debug.Log("FSTTshader: could not find object " + objectName);
                    }
                }
            }
        }

        private void setShader(Transform target)
        {
            target.gameObject.renderer.material = customShader;
            Debug.Log("FSTTshader: assigned custom shader to " + target.name);
            target.gameObject.renderer.material.SetColor("_Color", new Color(color.x, color.y, color.z, color.w));
        }
    }
}
