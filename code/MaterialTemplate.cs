namespace Facepunch.Tools;

public static class MaterialTemplate
{
	public static string String = @"
// THIS FILE IS AUTO-GENERATED

Layer0
{
	shader ""complex.shader""

	//---- PBR ----
	F_METALNESS_TEXTURE {UseMetalness}
	F_SPECULAR {Specular}

	//---- Ambient Occlusion ----
	g_flAmbientOcclusionDirectDiffuse ""0.000""
	g_flAmbientOcclusionDirectSpecular ""0.000""
	TextureAmbientOcclusion ""{AmbientOcclusion}""

	//---- Color ----
	g_flModelTintAmount ""1.000""
	g_vColorTint ""[1.000000 1.000000 1.000000 0.000000]""
	TextureColor ""{Albedo}""

	//---- Fade ----
	g_flFadeExponent ""1.000""

	//---- Fog ----
	g_bFogEnabled ""1""

	//---- Lighting ----
	g_flDirectionalLightmapMinZ ""0.050""
	g_flDirectionalLightmapStrength ""1.000""

	//---- Metalness ----
	g_flMetalness ""0.000""

	//---- Normal ----
	TextureNormal ""{Normal}""

	//---- Roughness ----
	TextureRoughness ""{Roughness}""

	//---- Metalness ----
	TextureMetalness ""{Metalness}""

	//---- Texture Coordinates ----
	g_nScaleTexCoordUByModelScaleAxis ""0""
	g_nScaleTexCoordVByModelScaleAxis ""0""
	g_vTexCoordOffset ""[0.000 0.000]""
	g_vTexCoordScale ""[1.000 1.000]""
	g_vTexCoordScrollSpeed ""[0.000 0.000]""
}
";
}
