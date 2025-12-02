

void SkinLighting_half(float3 WorldPos, float3 ViewDir, float3 WarpNormal, float3 Albedo, float Occlusion, float3 SSSColor, float Translucency, out half3 FinalColor)
{
#if SHADERGRAPH_PREVIEW
	Albedo = half3(1,1,1);
	WarpNormal = float3(0,0,1);
	Occlusion = 0.5;
	SSSColor = half3(0.8,0.7,0.6);
	Translucency = 0.6;
	FinalColor = half3(0.65,0.65,0.65);
#else
#if SHADOWS_SCREEN
	half4 clipPos = TransformWorldToHClip(WorldPos);
	half4 shadowCoord = ComputeScreenPos(clipPos);
#else
	half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif

	WarpNormal = normalize(WarpNormal);
	ViewDir = SafeNormalize(ViewDir);
   
	//MAIN LIGHTING CYCLE
	
	Light light = GetMainLight(shadowCoord);

	half3 softNormal = WarpNormal;
	half realNL = saturate(dot(softNormal, light.direction));
	half nl = saturate(dot(softNormal, light.direction)*0.65+0.3);
	half nv = abs(dot(WarpNormal, ViewDir));

	half3 transColor = nv*saturate(lerp(pow(Albedo,4)*2,pow(SSSColor,3)*1.5,saturate(pow(dot(light.direction,ViewDir),4))))*(1-nl);

	half3 scatter = lerp(0,saturate(Albedo*2*SSSColor), saturate(pow(1-realNL,3))) * light.color * light.shadowAttenuation * light.distanceAttenuation * realNL;

	scatter += saturate(transColor*Translucency*8*light.color*light.shadowAttenuation * light.distanceAttenuation);

	
	int pixelLightCount = GetAdditionalLightsCount();

	for (int i = 0; i < pixelLightCount; i++){
		Light light2 = GetAdditionalLight(i, WorldPos);
		realNL = saturate(dot(softNormal, light2.direction));
		nl = saturate(dot(softNormal, light2.direction)*0.65+0.3);
		nv = abs(dot(WarpNormal, ViewDir));
		transColor += nv*saturate(lerp(pow(Albedo,4)*2,pow(SSSColor,3)*1.5,saturate(pow(dot(light2.direction,ViewDir),4))))*(1-nl);
		half3 attenuatedLightColor = light2.color * (light2.distanceAttenuation * light2.shadowAttenuation);
		scatter += lerp(0,saturate(Albedo*2*SSSColor), saturate(pow(1-realNL,2))) * attenuatedLightColor * nl;
		scatter += saturate(transColor*Translucency*4*attenuatedLightColor);
	}

	FinalColor = saturate(scatter);
#endif
}


float4 MulQH( float4 q1, float4 q2 ){
			
			float4 q = {
				q1.x*q2.x - q1.y*q2.y - q1.z*q2.z - q1.w*q2.w,
				q1.x*q2.y + q1.y*q2.x + q1.z*q2.w - q1.w*q2.z,
				q1.x*q2.z - q1.y*q2.w + q1.z*q2.x + q1.w*q2.y,
				q1.x*q2.w + q1.y*q2.z - q1.z*q2.y + q1.w*q2.x
			};

			return q;
		}


		float3 rVQ(float3 v, float4 q){
		
			float4 vecQ = float4(0, v.xyz);
			float4 invQ = float4(q.x,-q.y,-q.z,-q.w);
			return MulQH(MulQH(q,vecQ),invQ).yzw;

		}


void CalcTensionMap_float(float4 texcoord1, float4 texcoord2, float4 texcoord3, float3 worldPos, float3 hBPos, float4 hBRot, float vThreshold, out float TensionMap){

#if SHADERGRAPH_PREVIEW
	texcoord1 = float4(0,0,0,0);
	texcoord2 = float4(0,0,0,0);
	texcoord3 = float4(0,0,0,0);
	TensionMap = 0;
#else
	
	float3 restPose = float3(texcoord1.z,texcoord1.w,texcoord2.x);
	float3 expPoseA = float3(texcoord2.y,texcoord2.z,texcoord2.w);
	float3 expPoseB = float3(texcoord3.x,texcoord3.y,texcoord3.z);
	float3 vertexPos = worldPos-hBPos.xyz;
			

	vertexPos = rVQ(vertexPos,hBRot);

	float distanceToRestPose = distance(vertexPos.xyz, restPose);
	float distanceToPoseA = distance(vertexPos.xyz, expPoseA)+vThreshold;
	float distanceToPoseB = distance(vertexPos.xyz, expPoseB)+vThreshold;
	float restPoseToPoseA = distance(restPose, expPoseA);
	float restPoseToPoseB = distance(restPose, expPoseB);

	float nRestToA = distanceToPoseA / restPoseToPoseA;
	float nRestToB = distanceToPoseB / restPoseToPoseB;

	TensionMap = saturate(1.2-min(nRestToA,nRestToB));

	TensionMap = saturate(pow(TensionMap*1.5,1.5));

#endif
	
}