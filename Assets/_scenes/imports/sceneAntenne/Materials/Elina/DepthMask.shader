//Shader "Custom/DepthMask"
//{
//SubShader
//    {
//       Tags{"Queue" = "Geometry-10"}
//   // Tags { "Queue"="Geometry-10" "RenderType"="Opaque" }
//      Lighting Off
//      Ztest LEqual
    
//       ZWrite On
//       ColorMask 0
//       Pass {}
//    }
//}


Shader "Custom/DepthMask"
{
    SubShader
    {
        Tags { "Queue" = "Geometry-10" "RenderType" = "Opaque" }
        ColorMask 0
        ZWrite On
        ZTest LEqual

        Stencil
        {
            Ref 1          // valeur d’écriture
            Comp always    // toujours écrire
            Pass replace   // remplace la valeur du stencil
        }

        Pass {}
    }
}
