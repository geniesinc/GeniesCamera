using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Genies.Naf
{
    /**
     * Utility class that maps Unity's GraphicsFormat enum to vulkan formats.
     *
     * Current source: https://github.com/KhronosGroup/Vulkan-Headers.git | Tag: v1.4.303
     */
    public static class VulkanFormat
    {
        public static string GetName(uint vulkanFormat)
        {
            return VulkanToName.GetValueOrDefault(vulkanFormat, nameof(VK_FORMAT_UNDEFINED));
        }
        
        public static GraphicsFormat GetGraphicsFormat(uint vulkanFormat)
        {
            return VulkanToUnityMap.GetValueOrDefault(vulkanFormat, GraphicsFormat.None);
        }
        
        public static uint GetVulkanFormat(GraphicsFormat format)
        {
            return UnityToVulkanMap.GetValueOrDefault(format, VK_FORMAT_UNDEFINED);
        }

        public static bool TryGetTextureFormat(uint vulkanFormat, out TextureFormat textureFormat, out bool linear)
        {
            GraphicsFormat format = GetGraphicsFormat(vulkanFormat);
            
            textureFormat = GraphicsFormatUtility.GetTextureFormat(format);
            linear        = !GraphicsFormatUtility.IsSRGBFormat(format);
            
            return format != GraphicsFormat.None;
        }
        
        public const uint VK_FORMAT_UNDEFINED                                  = 0;
        public const uint VK_FORMAT_R4G4_UNORM_PACK8                           = 1;
        public const uint VK_FORMAT_R4G4B4A4_UNORM_PACK16                      = 2;
        public const uint VK_FORMAT_B4G4R4A4_UNORM_PACK16                      = 3;
        public const uint VK_FORMAT_R5G6B5_UNORM_PACK16                        = 4;
        public const uint VK_FORMAT_B5G6R5_UNORM_PACK16                        = 5;
        public const uint VK_FORMAT_R5G5B5A1_UNORM_PACK16                      = 6;
        public const uint VK_FORMAT_B5G5R5A1_UNORM_PACK16                      = 7;
        public const uint VK_FORMAT_A1R5G5B5_UNORM_PACK16                      = 8;
        public const uint VK_FORMAT_R8_UNORM                                   = 9;
        public const uint VK_FORMAT_R8_SNORM                                   = 10;
        public const uint VK_FORMAT_R8_USCALED                                 = 11;
        public const uint VK_FORMAT_R8_SSCALED                                 = 12;
        public const uint VK_FORMAT_R8_UINT                                    = 13;
        public const uint VK_FORMAT_R8_SINT                                    = 14;
        public const uint VK_FORMAT_R8_SRGB                                    = 15;
        public const uint VK_FORMAT_R8G8_UNORM                                 = 16;
        public const uint VK_FORMAT_R8G8_SNORM                                 = 17;
        public const uint VK_FORMAT_R8G8_USCALED                               = 18;
        public const uint VK_FORMAT_R8G8_SSCALED                               = 19;
        public const uint VK_FORMAT_R8G8_UINT                                  = 20;
        public const uint VK_FORMAT_R8G8_SINT                                  = 21;
        public const uint VK_FORMAT_R8G8_SRGB                                  = 22;
        public const uint VK_FORMAT_R8G8B8_UNORM                               = 23;
        public const uint VK_FORMAT_R8G8B8_SNORM                               = 24;
        public const uint VK_FORMAT_R8G8B8_USCALED                             = 25;
        public const uint VK_FORMAT_R8G8B8_SSCALED                             = 26;
        public const uint VK_FORMAT_R8G8B8_UINT                                = 27;
        public const uint VK_FORMAT_R8G8B8_SINT                                = 28;
        public const uint VK_FORMAT_R8G8B8_SRGB                                = 29;
        public const uint VK_FORMAT_B8G8R8_UNORM                               = 30;
        public const uint VK_FORMAT_B8G8R8_SNORM                               = 31;
        public const uint VK_FORMAT_B8G8R8_USCALED                             = 32;
        public const uint VK_FORMAT_B8G8R8_SSCALED                             = 33;
        public const uint VK_FORMAT_B8G8R8_UINT                                = 34;
        public const uint VK_FORMAT_B8G8R8_SINT                                = 35;
        public const uint VK_FORMAT_B8G8R8_SRGB                                = 36;
        public const uint VK_FORMAT_R8G8B8A8_UNORM                             = 37;
        public const uint VK_FORMAT_R8G8B8A8_SNORM                             = 38;
        public const uint VK_FORMAT_R8G8B8A8_USCALED                           = 39;
        public const uint VK_FORMAT_R8G8B8A8_SSCALED                           = 40;
        public const uint VK_FORMAT_R8G8B8A8_UINT                              = 41;
        public const uint VK_FORMAT_R8G8B8A8_SINT                              = 42;
        public const uint VK_FORMAT_R8G8B8A8_SRGB                              = 43;
        public const uint VK_FORMAT_B8G8R8A8_UNORM                             = 44;
        public const uint VK_FORMAT_B8G8R8A8_SNORM                             = 45;
        public const uint VK_FORMAT_B8G8R8A8_USCALED                           = 46;
        public const uint VK_FORMAT_B8G8R8A8_SSCALED                           = 47;
        public const uint VK_FORMAT_B8G8R8A8_UINT                              = 48;
        public const uint VK_FORMAT_B8G8R8A8_SINT                              = 49;
        public const uint VK_FORMAT_B8G8R8A8_SRGB                              = 50;
        public const uint VK_FORMAT_A8B8G8R8_UNORM_PACK32                      = 51;
        public const uint VK_FORMAT_A8B8G8R8_SNORM_PACK32                      = 52;
        public const uint VK_FORMAT_A8B8G8R8_USCALED_PACK32                    = 53;
        public const uint VK_FORMAT_A8B8G8R8_SSCALED_PACK32                    = 54;
        public const uint VK_FORMAT_A8B8G8R8_UINT_PACK32                       = 55;
        public const uint VK_FORMAT_A8B8G8R8_SINT_PACK32                       = 56;
        public const uint VK_FORMAT_A8B8G8R8_SRGB_PACK32                       = 57;
        public const uint VK_FORMAT_A2R10G10B10_UNORM_PACK32                   = 58;
        public const uint VK_FORMAT_A2R10G10B10_SNORM_PACK32                   = 59;
        public const uint VK_FORMAT_A2R10G10B10_USCALED_PACK32                 = 60;
        public const uint VK_FORMAT_A2R10G10B10_SSCALED_PACK32                 = 61;
        public const uint VK_FORMAT_A2R10G10B10_UINT_PACK32                    = 62;
        public const uint VK_FORMAT_A2R10G10B10_SINT_PACK32                    = 63;
        public const uint VK_FORMAT_A2B10G10R10_UNORM_PACK32                   = 64;
        public const uint VK_FORMAT_A2B10G10R10_SNORM_PACK32                   = 65;
        public const uint VK_FORMAT_A2B10G10R10_USCALED_PACK32                 = 66;
        public const uint VK_FORMAT_A2B10G10R10_SSCALED_PACK32                 = 67;
        public const uint VK_FORMAT_A2B10G10R10_UINT_PACK32                    = 68;
        public const uint VK_FORMAT_A2B10G10R10_SINT_PACK32                    = 69;
        public const uint VK_FORMAT_R16_UNORM                                  = 70;
        public const uint VK_FORMAT_R16_SNORM                                  = 71;
        public const uint VK_FORMAT_R16_USCALED                                = 72;
        public const uint VK_FORMAT_R16_SSCALED                                = 73;
        public const uint VK_FORMAT_R16_UINT                                   = 74;
        public const uint VK_FORMAT_R16_SINT                                   = 75;
        public const uint VK_FORMAT_R16_SFLOAT                                 = 76;
        public const uint VK_FORMAT_R16G16_UNORM                               = 77;
        public const uint VK_FORMAT_R16G16_SNORM                               = 78;
        public const uint VK_FORMAT_R16G16_USCALED                             = 79;
        public const uint VK_FORMAT_R16G16_SSCALED                             = 80;
        public const uint VK_FORMAT_R16G16_UINT                                = 81;
        public const uint VK_FORMAT_R16G16_SINT                                = 82;
        public const uint VK_FORMAT_R16G16_SFLOAT                              = 83;
        public const uint VK_FORMAT_R16G16B16_UNORM                            = 84;
        public const uint VK_FORMAT_R16G16B16_SNORM                            = 85;
        public const uint VK_FORMAT_R16G16B16_USCALED                          = 86;
        public const uint VK_FORMAT_R16G16B16_SSCALED                          = 87;
        public const uint VK_FORMAT_R16G16B16_UINT                             = 88;
        public const uint VK_FORMAT_R16G16B16_SINT                             = 89;
        public const uint VK_FORMAT_R16G16B16_SFLOAT                           = 90;
        public const uint VK_FORMAT_R16G16B16A16_UNORM                         = 91;
        public const uint VK_FORMAT_R16G16B16A16_SNORM                         = 92;
        public const uint VK_FORMAT_R16G16B16A16_USCALED                       = 93;
        public const uint VK_FORMAT_R16G16B16A16_SSCALED                       = 94;
        public const uint VK_FORMAT_R16G16B16A16_UINT                          = 95;
        public const uint VK_FORMAT_R16G16B16A16_SINT                          = 96;
        public const uint VK_FORMAT_R16G16B16A16_SFLOAT                        = 97;
        public const uint VK_FORMAT_R32_UINT                                   = 98;
        public const uint VK_FORMAT_R32_SINT                                   = 99;
        public const uint VK_FORMAT_R32_SFLOAT                                 = 100;
        public const uint VK_FORMAT_R32G32_UINT                                = 101;
        public const uint VK_FORMAT_R32G32_SINT                                = 102;
        public const uint VK_FORMAT_R32G32_SFLOAT                              = 103;
        public const uint VK_FORMAT_R32G32B32_UINT                             = 104;
        public const uint VK_FORMAT_R32G32B32_SINT                             = 105;
        public const uint VK_FORMAT_R32G32B32_SFLOAT                           = 106;
        public const uint VK_FORMAT_R32G32B32A32_UINT                          = 107;
        public const uint VK_FORMAT_R32G32B32A32_SINT                          = 108;
        public const uint VK_FORMAT_R32G32B32A32_SFLOAT                        = 109;
        public const uint VK_FORMAT_R64_UINT                                   = 110;
        public const uint VK_FORMAT_R64_SINT                                   = 111;
        public const uint VK_FORMAT_R64_SFLOAT                                 = 112;
        public const uint VK_FORMAT_R64G64_UINT                                = 113;
        public const uint VK_FORMAT_R64G64_SINT                                = 114;
        public const uint VK_FORMAT_R64G64_SFLOAT                              = 115;
        public const uint VK_FORMAT_R64G64B64_UINT                             = 116;
        public const uint VK_FORMAT_R64G64B64_SINT                             = 117;
        public const uint VK_FORMAT_R64G64B64_SFLOAT                           = 118;
        public const uint VK_FORMAT_R64G64B64A64_UINT                          = 119;
        public const uint VK_FORMAT_R64G64B64A64_SINT                          = 120;
        public const uint VK_FORMAT_R64G64B64A64_SFLOAT                        = 121;
        public const uint VK_FORMAT_B10G11R11_UFLOAT_PACK32                    = 122;
        public const uint VK_FORMAT_E5B9G9R9_UFLOAT_PACK32                     = 123;
        public const uint VK_FORMAT_D16_UNORM                                  = 124;
        public const uint VK_FORMAT_X8_D24_UNORM_PACK32                        = 125;
        public const uint VK_FORMAT_D32_SFLOAT                                 = 126;
        public const uint VK_FORMAT_S8_UINT                                    = 127;
        public const uint VK_FORMAT_D16_UNORM_S8_UINT                          = 128;
        public const uint VK_FORMAT_D24_UNORM_S8_UINT                          = 129;
        public const uint VK_FORMAT_D32_SFLOAT_S8_UINT                         = 130;
        public const uint VK_FORMAT_BC1_RGB_UNORM_BLOCK                        = 131;
        public const uint VK_FORMAT_BC1_RGB_SRGB_BLOCK                         = 132;
        public const uint VK_FORMAT_BC1_RGBA_UNORM_BLOCK                       = 133;
        public const uint VK_FORMAT_BC1_RGBA_SRGB_BLOCK                        = 134;
        public const uint VK_FORMAT_BC2_UNORM_BLOCK                            = 135;
        public const uint VK_FORMAT_BC2_SRGB_BLOCK                             = 136;
        public const uint VK_FORMAT_BC3_UNORM_BLOCK                            = 137;
        public const uint VK_FORMAT_BC3_SRGB_BLOCK                             = 138;
        public const uint VK_FORMAT_BC4_UNORM_BLOCK                            = 139;
        public const uint VK_FORMAT_BC4_SNORM_BLOCK                            = 140;
        public const uint VK_FORMAT_BC5_UNORM_BLOCK                            = 141;
        public const uint VK_FORMAT_BC5_SNORM_BLOCK                            = 142;
        public const uint VK_FORMAT_BC6H_UFLOAT_BLOCK                          = 143;
        public const uint VK_FORMAT_BC6H_SFLOAT_BLOCK                          = 144;
        public const uint VK_FORMAT_BC7_UNORM_BLOCK                            = 145;
        public const uint VK_FORMAT_BC7_SRGB_BLOCK                             = 146;
        public const uint VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK                    = 147;
        public const uint VK_FORMAT_ETC2_R8G8B8_SRGB_BLOCK                     = 148;
        public const uint VK_FORMAT_ETC2_R8G8B8A1_UNORM_BLOCK                  = 149;
        public const uint VK_FORMAT_ETC2_R8G8B8A1_SRGB_BLOCK                   = 150;
        public const uint VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK                  = 151;
        public const uint VK_FORMAT_ETC2_R8G8B8A8_SRGB_BLOCK                   = 152;
        public const uint VK_FORMAT_EAC_R11_UNORM_BLOCK                        = 153;
        public const uint VK_FORMAT_EAC_R11_SNORM_BLOCK                        = 154;
        public const uint VK_FORMAT_EAC_R11G11_UNORM_BLOCK                     = 155;
        public const uint VK_FORMAT_EAC_R11G11_SNORM_BLOCK                     = 156;
        public const uint VK_FORMAT_ASTC_4x4_UNORM_BLOCK                       = 157;
        public const uint VK_FORMAT_ASTC_4x4_SRGB_BLOCK                        = 158;
        public const uint VK_FORMAT_ASTC_5x4_UNORM_BLOCK                       = 159;
        public const uint VK_FORMAT_ASTC_5x4_SRGB_BLOCK                        = 160;
        public const uint VK_FORMAT_ASTC_5x5_UNORM_BLOCK                       = 161;
        public const uint VK_FORMAT_ASTC_5x5_SRGB_BLOCK                        = 162;
        public const uint VK_FORMAT_ASTC_6x5_UNORM_BLOCK                       = 163;
        public const uint VK_FORMAT_ASTC_6x5_SRGB_BLOCK                        = 164;
        public const uint VK_FORMAT_ASTC_6x6_UNORM_BLOCK                       = 165;
        public const uint VK_FORMAT_ASTC_6x6_SRGB_BLOCK                        = 166;
        public const uint VK_FORMAT_ASTC_8x5_UNORM_BLOCK                       = 167;
        public const uint VK_FORMAT_ASTC_8x5_SRGB_BLOCK                        = 168;
        public const uint VK_FORMAT_ASTC_8x6_UNORM_BLOCK                       = 169;
        public const uint VK_FORMAT_ASTC_8x6_SRGB_BLOCK                        = 170;
        public const uint VK_FORMAT_ASTC_8x8_UNORM_BLOCK                       = 171;
        public const uint VK_FORMAT_ASTC_8x8_SRGB_BLOCK                        = 172;
        public const uint VK_FORMAT_ASTC_10x5_UNORM_BLOCK                      = 173;
        public const uint VK_FORMAT_ASTC_10x5_SRGB_BLOCK                       = 174;
        public const uint VK_FORMAT_ASTC_10x6_UNORM_BLOCK                      = 175;
        public const uint VK_FORMAT_ASTC_10x6_SRGB_BLOCK                       = 176;
        public const uint VK_FORMAT_ASTC_10x8_UNORM_BLOCK                      = 177;
        public const uint VK_FORMAT_ASTC_10x8_SRGB_BLOCK                       = 178;
        public const uint VK_FORMAT_ASTC_10x10_UNORM_BLOCK                     = 179;
        public const uint VK_FORMAT_ASTC_10x10_SRGB_BLOCK                      = 180;
        public const uint VK_FORMAT_ASTC_12x10_UNORM_BLOCK                     = 181;
        public const uint VK_FORMAT_ASTC_12x10_SRGB_BLOCK                      = 182;
        public const uint VK_FORMAT_ASTC_12x12_UNORM_BLOCK                     = 183;
        public const uint VK_FORMAT_ASTC_12x12_SRGB_BLOCK                      = 184;
        public const uint VK_FORMAT_G8B8G8R8_422_UNORM                         = 1000156000;
        public const uint VK_FORMAT_B8G8R8G8_422_UNORM                         = 1000156001;
        public const uint VK_FORMAT_G8_B8_R8_3PLANE_420_UNORM                  = 1000156002;
        public const uint VK_FORMAT_G8_B8R8_2PLANE_420_UNORM                   = 1000156003;
        public const uint VK_FORMAT_G8_B8_R8_3PLANE_422_UNORM                  = 1000156004;
        public const uint VK_FORMAT_G8_B8R8_2PLANE_422_UNORM                   = 1000156005;
        public const uint VK_FORMAT_G8_B8_R8_3PLANE_444_UNORM                  = 1000156006;
        public const uint VK_FORMAT_R10X6_UNORM_PACK16                         = 1000156007;
        public const uint VK_FORMAT_R10X6G10X6_UNORM_2PACK16                   = 1000156008;
        public const uint VK_FORMAT_R10X6G10X6B10X6A10X6_UNORM_4PACK16         = 1000156009;
        public const uint VK_FORMAT_G10X6B10X6G10X6R10X6_422_UNORM_4PACK16     = 1000156010;
        public const uint VK_FORMAT_B10X6G10X6R10X6G10X6_422_UNORM_4PACK16     = 1000156011;
        public const uint VK_FORMAT_G10X6_B10X6_R10X6_3PLANE_420_UNORM_3PACK16 = 1000156012;
        public const uint VK_FORMAT_G10X6_B10X6R10X6_2PLANE_420_UNORM_3PACK16  = 1000156013;
        public const uint VK_FORMAT_G10X6_B10X6_R10X6_3PLANE_422_UNORM_3PACK16 = 1000156014;
        public const uint VK_FORMAT_G10X6_B10X6R10X6_2PLANE_422_UNORM_3PACK16  = 1000156015;
        public const uint VK_FORMAT_G10X6_B10X6_R10X6_3PLANE_444_UNORM_3PACK16 = 1000156016;
        public const uint VK_FORMAT_R12X4_UNORM_PACK16                         = 1000156017;
        public const uint VK_FORMAT_R12X4G12X4_UNORM_2PACK16                   = 1000156018;
        public const uint VK_FORMAT_R12X4G12X4B12X4A12X4_UNORM_4PACK16         = 1000156019;
        public const uint VK_FORMAT_G12X4B12X4G12X4R12X4_422_UNORM_4PACK16     = 1000156020;
        public const uint VK_FORMAT_B12X4G12X4R12X4G12X4_422_UNORM_4PACK16     = 1000156021;
        public const uint VK_FORMAT_G12X4_B12X4_R12X4_3PLANE_420_UNORM_3PACK16 = 1000156022;
        public const uint VK_FORMAT_G12X4_B12X4R12X4_2PLANE_420_UNORM_3PACK16  = 1000156023;
        public const uint VK_FORMAT_G12X4_B12X4_R12X4_3PLANE_422_UNORM_3PACK16 = 1000156024;
        public const uint VK_FORMAT_G12X4_B12X4R12X4_2PLANE_422_UNORM_3PACK16  = 1000156025;
        public const uint VK_FORMAT_G12X4_B12X4_R12X4_3PLANE_444_UNORM_3PACK16 = 1000156026;
        public const uint VK_FORMAT_G16B16G16R16_422_UNORM                     = 1000156027;
        public const uint VK_FORMAT_B16G16R16G16_422_UNORM                     = 1000156028;
        public const uint VK_FORMAT_G16_B16_R16_3PLANE_420_UNORM               = 1000156029;
        public const uint VK_FORMAT_G16_B16R16_2PLANE_420_UNORM                = 1000156030;
        public const uint VK_FORMAT_G16_B16_R16_3PLANE_422_UNORM               = 1000156031;
        public const uint VK_FORMAT_G16_B16R16_2PLANE_422_UNORM                = 1000156032;
        public const uint VK_FORMAT_G16_B16_R16_3PLANE_444_UNORM               = 1000156033;
        public const uint VK_FORMAT_G8_B8R8_2PLANE_444_UNORM                   = 1000330000;
        public const uint VK_FORMAT_G10X6_B10X6R10X6_2PLANE_444_UNORM_3PACK16  = 1000330001;
        public const uint VK_FORMAT_G12X4_B12X4R12X4_2PLANE_444_UNORM_3PACK16  = 1000330002;
        public const uint VK_FORMAT_G16_B16R16_2PLANE_444_UNORM                = 1000330003;
        public const uint VK_FORMAT_A4R4G4B4_UNORM_PACK16                      = 1000340000;
        public const uint VK_FORMAT_A4B4G4R4_UNORM_PACK16                      = 1000340001;
        public const uint VK_FORMAT_ASTC_4x4_SFLOAT_BLOCK                      = 1000066000;
        public const uint VK_FORMAT_ASTC_5x4_SFLOAT_BLOCK                      = 1000066001;
        public const uint VK_FORMAT_ASTC_5x5_SFLOAT_BLOCK                      = 1000066002;
        public const uint VK_FORMAT_ASTC_6x5_SFLOAT_BLOCK                      = 1000066003;
        public const uint VK_FORMAT_ASTC_6x6_SFLOAT_BLOCK                      = 1000066004;
        public const uint VK_FORMAT_ASTC_8x5_SFLOAT_BLOCK                      = 1000066005;
        public const uint VK_FORMAT_ASTC_8x6_SFLOAT_BLOCK                      = 1000066006;
        public const uint VK_FORMAT_ASTC_8x8_SFLOAT_BLOCK                      = 1000066007;
        public const uint VK_FORMAT_ASTC_10x5_SFLOAT_BLOCK                     = 1000066008;
        public const uint VK_FORMAT_ASTC_10x6_SFLOAT_BLOCK                     = 1000066009;
        public const uint VK_FORMAT_ASTC_10x8_SFLOAT_BLOCK                     = 1000066010;
        public const uint VK_FORMAT_ASTC_10x10_SFLOAT_BLOCK                    = 1000066011;
        public const uint VK_FORMAT_ASTC_12x10_SFLOAT_BLOCK                    = 1000066012;
        public const uint VK_FORMAT_ASTC_12x12_SFLOAT_BLOCK                    = 1000066013;
        public const uint VK_FORMAT_A1B5G5R5_UNORM_PACK16                      = 1000470000;
        public const uint VK_FORMAT_A8_UNORM                                   = 1000470001;
        public const uint VK_FORMAT_PVRTC1_2BPP_UNORM_BLOCK_IMG                = 1000054000;
        public const uint VK_FORMAT_PVRTC1_4BPP_UNORM_BLOCK_IMG                = 1000054001;
        public const uint VK_FORMAT_PVRTC2_2BPP_UNORM_BLOCK_IMG                = 1000054002;
        public const uint VK_FORMAT_PVRTC2_4BPP_UNORM_BLOCK_IMG                = 1000054003;
        public const uint VK_FORMAT_PVRTC1_2BPP_SRGB_BLOCK_IMG                 = 1000054004;
        public const uint VK_FORMAT_PVRTC1_4BPP_SRGB_BLOCK_IMG                 = 1000054005;
        public const uint VK_FORMAT_PVRTC2_2BPP_SRGB_BLOCK_IMG                 = 1000054006;
        public const uint VK_FORMAT_PVRTC2_4BPP_SRGB_BLOCK_IMG                 = 1000054007;
        public const uint VK_FORMAT_R16G16_SFIXED5_NV                          = 1000464000;
        
        public static readonly Dictionary<uint, string> VulkanToName = typeof(VulkanFormat)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(uint))
            .ToDictionary(field => (uint)field.GetRawConstantValue(), field => field.Name);

        public static readonly Dictionary<uint, GraphicsFormat> VulkanToUnityMap = new()
        {
            { VK_FORMAT_UNDEFINED,                   GraphicsFormat.None },
            { VK_FORMAT_R8_SRGB,                     GraphicsFormat.R8_SRGB },
            { VK_FORMAT_R8G8_SRGB,                   GraphicsFormat.R8G8_SRGB },
            { VK_FORMAT_R8G8B8_SRGB,                 GraphicsFormat.R8G8B8_SRGB },
            { VK_FORMAT_R8G8B8A8_SRGB,               GraphicsFormat.R8G8B8A8_SRGB },
            { VK_FORMAT_R8_UNORM,                    GraphicsFormat.R8_UNorm },
            { VK_FORMAT_R8G8_UNORM,                  GraphicsFormat.R8G8_UNorm },
            { VK_FORMAT_R8G8B8_UNORM,                GraphicsFormat.R8G8B8_UNorm },
            { VK_FORMAT_R8G8B8A8_UNORM,              GraphicsFormat.R8G8B8A8_UNorm },
            { VK_FORMAT_R8_SNORM,                    GraphicsFormat.R8_SNorm },
            { VK_FORMAT_R8G8_SNORM,                  GraphicsFormat.R8G8_SNorm },
            { VK_FORMAT_R8G8B8_SNORM,                GraphicsFormat.R8G8B8_SNorm },
            { VK_FORMAT_R8G8B8A8_SNORM,              GraphicsFormat.R8G8B8A8_SNorm },
            { VK_FORMAT_R8_UINT,                     GraphicsFormat.R8_UInt },
            { VK_FORMAT_R8G8_UINT,                   GraphicsFormat.R8G8_UInt },
            { VK_FORMAT_R8G8B8_UINT,                 GraphicsFormat.R8G8B8_UInt },
            { VK_FORMAT_R8G8B8A8_UINT,               GraphicsFormat.R8G8B8A8_UInt },
            { VK_FORMAT_R8_SINT,                     GraphicsFormat.R8_SInt },
            { VK_FORMAT_R8G8_SINT,                   GraphicsFormat.R8G8_SInt },
            { VK_FORMAT_R8G8B8_SINT,                 GraphicsFormat.R8G8B8_SInt },
            { VK_FORMAT_R8G8B8A8_SINT,               GraphicsFormat.R8G8B8A8_SInt },
            { VK_FORMAT_R16_UNORM,                   GraphicsFormat.R16_UNorm },
            { VK_FORMAT_R16G16_UNORM,                GraphicsFormat.R16G16_UNorm },
            { VK_FORMAT_R16G16B16_UNORM,             GraphicsFormat.R16G16B16_UNorm },
            { VK_FORMAT_R16G16B16A16_UNORM,          GraphicsFormat.R16G16B16A16_UNorm },
            { VK_FORMAT_R16_SNORM,                   GraphicsFormat.R16_SNorm },
            { VK_FORMAT_R16G16_SNORM,                GraphicsFormat.R16G16_SNorm },
            { VK_FORMAT_R16G16B16_SNORM,             GraphicsFormat.R16G16B16_SNorm },
            { VK_FORMAT_R16G16B16A16_SNORM,          GraphicsFormat.R16G16B16A16_SNorm },
            { VK_FORMAT_R16_UINT,                    GraphicsFormat.R16_UInt },
            { VK_FORMAT_R16G16_UINT,                 GraphicsFormat.R16G16_UInt },
            { VK_FORMAT_R16G16B16_UINT,              GraphicsFormat.R16G16B16_UInt },
            { VK_FORMAT_R16G16B16A16_UINT,           GraphicsFormat.R16G16B16A16_UInt },
            { VK_FORMAT_R16_SINT,                    GraphicsFormat.R16_SInt },
            { VK_FORMAT_R16G16_SINT,                 GraphicsFormat.R16G16_SInt },
            { VK_FORMAT_R16G16B16_SINT,              GraphicsFormat.R16G16B16_SInt },
            { VK_FORMAT_R16G16B16A16_SINT,           GraphicsFormat.R16G16B16A16_SInt },
            { VK_FORMAT_R32_UINT,                    GraphicsFormat.R32_UInt },
            { VK_FORMAT_R32G32_UINT,                 GraphicsFormat.R32G32_UInt },
            { VK_FORMAT_R32G32B32_UINT,              GraphicsFormat.R32G32B32_UInt },
            { VK_FORMAT_R32G32B32A32_UINT,           GraphicsFormat.R32G32B32A32_UInt },
            { VK_FORMAT_R32_SINT,                    GraphicsFormat.R32_SInt },
            { VK_FORMAT_R32G32_SINT,                 GraphicsFormat.R32G32_SInt },
            { VK_FORMAT_R32G32B32_SINT,              GraphicsFormat.R32G32B32_SInt },
            { VK_FORMAT_R32G32B32A32_SINT,           GraphicsFormat.R32G32B32A32_SInt },
            { VK_FORMAT_R16_SFLOAT,                  GraphicsFormat.R16_SFloat },
            { VK_FORMAT_R16G16_SFLOAT,               GraphicsFormat.R16G16_SFloat },
            { VK_FORMAT_R16G16B16_SFLOAT,            GraphicsFormat.R16G16B16_SFloat },
            { VK_FORMAT_R16G16B16A16_SFLOAT,         GraphicsFormat.R16G16B16A16_SFloat },
            { VK_FORMAT_R32_SFLOAT,                  GraphicsFormat.R32_SFloat },
            { VK_FORMAT_R32G32_SFLOAT,               GraphicsFormat.R32G32_SFloat },
            { VK_FORMAT_R32G32B32_SFLOAT,            GraphicsFormat.R32G32B32_SFloat },
            { VK_FORMAT_R32G32B32A32_SFLOAT,         GraphicsFormat.R32G32B32A32_SFloat },
            { VK_FORMAT_B8G8R8_SRGB,                 GraphicsFormat.B8G8R8_SRGB },
            { VK_FORMAT_B8G8R8A8_SRGB,               GraphicsFormat.B8G8R8A8_SRGB },
            { VK_FORMAT_B8G8R8_UNORM,                GraphicsFormat.B8G8R8_UNorm },
            { VK_FORMAT_B8G8R8A8_UNORM,              GraphicsFormat.B8G8R8A8_UNorm },
            { VK_FORMAT_B8G8R8_SNORM,                GraphicsFormat.B8G8R8_SNorm },
            { VK_FORMAT_B8G8R8A8_SNORM,              GraphicsFormat.B8G8R8A8_SNorm },
            { VK_FORMAT_B8G8R8_UINT,                 GraphicsFormat.B8G8R8_UInt },
            { VK_FORMAT_B8G8R8A8_UINT,               GraphicsFormat.B8G8R8A8_UInt },
            { VK_FORMAT_B8G8R8_SINT,                 GraphicsFormat.B8G8R8_SInt },
            { VK_FORMAT_B8G8R8A8_SINT,               GraphicsFormat.B8G8R8A8_SInt },
            { VK_FORMAT_R4G4B4A4_UNORM_PACK16,       GraphicsFormat.R4G4B4A4_UNormPack16 },
            { VK_FORMAT_B4G4R4A4_UNORM_PACK16,       GraphicsFormat.B4G4R4A4_UNormPack16 },
            { VK_FORMAT_R5G6B5_UNORM_PACK16,         GraphicsFormat.R5G6B5_UNormPack16 },
            { VK_FORMAT_B5G6R5_UNORM_PACK16,         GraphicsFormat.B5G6R5_UNormPack16 },
            { VK_FORMAT_R5G5B5A1_UNORM_PACK16,       GraphicsFormat.R5G5B5A1_UNormPack16 },
            { VK_FORMAT_B5G5R5A1_UNORM_PACK16,       GraphicsFormat.B5G5R5A1_UNormPack16 },
            { VK_FORMAT_A1R5G5B5_UNORM_PACK16,       GraphicsFormat.A1R5G5B5_UNormPack16 },
            { VK_FORMAT_E5B9G9R9_UFLOAT_PACK32,      GraphicsFormat.E5B9G9R9_UFloatPack32 },
            { VK_FORMAT_B10G11R11_UFLOAT_PACK32,     GraphicsFormat.B10G11R11_UFloatPack32 },
            { VK_FORMAT_A2B10G10R10_UNORM_PACK32,    GraphicsFormat.A2B10G10R10_UNormPack32 },
            { VK_FORMAT_A2B10G10R10_UINT_PACK32,     GraphicsFormat.A2B10G10R10_UIntPack32 },
            { VK_FORMAT_A2B10G10R10_SINT_PACK32,     GraphicsFormat.A2B10G10R10_SIntPack32 },
            { VK_FORMAT_A2R10G10B10_UNORM_PACK32,    GraphicsFormat.A2R10G10B10_UNormPack32 },
            { VK_FORMAT_A2R10G10B10_UINT_PACK32,     GraphicsFormat.A2R10G10B10_UIntPack32 },
            { VK_FORMAT_A2R10G10B10_SINT_PACK32,     GraphicsFormat.A2R10G10B10_SIntPack32 },
            { VK_FORMAT_D16_UNORM,                   GraphicsFormat.D16_UNorm },
            { VK_FORMAT_D24_UNORM_S8_UINT,           GraphicsFormat.D24_UNorm_S8_UInt },
            { VK_FORMAT_D32_SFLOAT,                  GraphicsFormat.D32_SFloat },
            { VK_FORMAT_D32_SFLOAT_S8_UINT,          GraphicsFormat.D32_SFloat_S8_UInt },
            { VK_FORMAT_S8_UINT,                     GraphicsFormat.S8_UInt },
            { VK_FORMAT_BC1_RGB_SRGB_BLOCK,          GraphicsFormat.RGBA_DXT1_SRGB },
            { VK_FORMAT_BC1_RGB_UNORM_BLOCK,         GraphicsFormat.RGBA_DXT1_UNorm },
            { VK_FORMAT_BC1_RGBA_SRGB_BLOCK,         GraphicsFormat.RGBA_DXT1_SRGB },
            { VK_FORMAT_BC1_RGBA_UNORM_BLOCK,        GraphicsFormat.RGBA_DXT1_UNorm },
            { VK_FORMAT_BC2_SRGB_BLOCK,              GraphicsFormat.RGBA_DXT3_SRGB },
            { VK_FORMAT_BC2_UNORM_BLOCK,             GraphicsFormat.RGBA_DXT3_UNorm },
            { VK_FORMAT_BC3_SRGB_BLOCK,              GraphicsFormat.RGBA_DXT5_SRGB },
            { VK_FORMAT_BC3_UNORM_BLOCK,             GraphicsFormat.RGBA_DXT5_UNorm },
            { VK_FORMAT_BC4_UNORM_BLOCK,             GraphicsFormat.R_BC4_UNorm },
            { VK_FORMAT_BC4_SNORM_BLOCK,             GraphicsFormat.R_BC4_SNorm },
            { VK_FORMAT_BC5_UNORM_BLOCK,             GraphicsFormat.RG_BC5_UNorm },
            { VK_FORMAT_BC5_SNORM_BLOCK,             GraphicsFormat.RG_BC5_SNorm },
            { VK_FORMAT_BC6H_UFLOAT_BLOCK,           GraphicsFormat.RGB_BC6H_UFloat },
            { VK_FORMAT_BC6H_SFLOAT_BLOCK,           GraphicsFormat.RGB_BC6H_SFloat },
            { VK_FORMAT_BC7_SRGB_BLOCK,              GraphicsFormat.RGBA_BC7_SRGB },
            { VK_FORMAT_BC7_UNORM_BLOCK,             GraphicsFormat.RGBA_BC7_UNorm },
            { VK_FORMAT_ETC2_R8G8B8_SRGB_BLOCK,      GraphicsFormat.RGB_ETC2_SRGB },
            { VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK,     GraphicsFormat.RGB_ETC2_UNorm },
            { VK_FORMAT_ETC2_R8G8B8A1_SRGB_BLOCK,    GraphicsFormat.RGB_A1_ETC2_SRGB },
            { VK_FORMAT_ETC2_R8G8B8A1_UNORM_BLOCK,   GraphicsFormat.RGB_A1_ETC2_UNorm },
            { VK_FORMAT_ETC2_R8G8B8A8_SRGB_BLOCK,    GraphicsFormat.RGBA_ETC2_SRGB },
            { VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK,   GraphicsFormat.RGBA_ETC2_UNorm },
            { VK_FORMAT_EAC_R11_UNORM_BLOCK,         GraphicsFormat.R_EAC_UNorm },
            { VK_FORMAT_EAC_R11_SNORM_BLOCK,         GraphicsFormat.R_EAC_SNorm },
            { VK_FORMAT_EAC_R11G11_UNORM_BLOCK,      GraphicsFormat.RG_EAC_UNorm },
            { VK_FORMAT_EAC_R11G11_SNORM_BLOCK,      GraphicsFormat.RG_EAC_SNorm },
            { VK_FORMAT_ASTC_4x4_SRGB_BLOCK,         GraphicsFormat.RGBA_ASTC4X4_SRGB },
            { VK_FORMAT_ASTC_4x4_UNORM_BLOCK,        GraphicsFormat.RGBA_ASTC4X4_UNorm },
            { VK_FORMAT_ASTC_5x5_SRGB_BLOCK,         GraphicsFormat.RGBA_ASTC5X5_SRGB },
            { VK_FORMAT_ASTC_5x5_UNORM_BLOCK,        GraphicsFormat.RGBA_ASTC5X5_UNorm },
            { VK_FORMAT_ASTC_6x6_SRGB_BLOCK,         GraphicsFormat.RGBA_ASTC6X6_SRGB },
            { VK_FORMAT_ASTC_6x6_UNORM_BLOCK,        GraphicsFormat.RGBA_ASTC6X6_UNorm },
            { VK_FORMAT_ASTC_8x8_SRGB_BLOCK,         GraphicsFormat.RGBA_ASTC8X8_SRGB },
            { VK_FORMAT_ASTC_8x8_UNORM_BLOCK,        GraphicsFormat.RGBA_ASTC8X8_UNorm },
            { VK_FORMAT_ASTC_10x10_SRGB_BLOCK,       GraphicsFormat.RGBA_ASTC10X10_SRGB },
            { VK_FORMAT_ASTC_10x10_UNORM_BLOCK,      GraphicsFormat.RGBA_ASTC10X10_UNorm },
            { VK_FORMAT_ASTC_12x12_SRGB_BLOCK,       GraphicsFormat.RGBA_ASTC12X12_SRGB },
            { VK_FORMAT_ASTC_12x12_UNORM_BLOCK,      GraphicsFormat.RGBA_ASTC12X12_UNorm },
            { VK_FORMAT_ASTC_4x4_SFLOAT_BLOCK,       GraphicsFormat.RGBA_ASTC4X4_UFloat },
            { VK_FORMAT_ASTC_5x5_SFLOAT_BLOCK,       GraphicsFormat.RGBA_ASTC5X5_UFloat },
            { VK_FORMAT_ASTC_6x6_SFLOAT_BLOCK,       GraphicsFormat.RGBA_ASTC6X6_UFloat },
            { VK_FORMAT_ASTC_8x8_SFLOAT_BLOCK,       GraphicsFormat.RGBA_ASTC8X8_UFloat },
            { VK_FORMAT_ASTC_10x10_SFLOAT_BLOCK,     GraphicsFormat.RGBA_ASTC10X10_UFloat },
            { VK_FORMAT_ASTC_12x12_SFLOAT_BLOCK,     GraphicsFormat.RGBA_ASTC12X12_UFloat },
            { VK_FORMAT_D16_UNORM_S8_UINT,           GraphicsFormat.D16_UNorm_S8_UInt },
        };
        
        public static readonly Dictionary<GraphicsFormat, uint> UnityToVulkanMap = new()
        {
            { GraphicsFormat.None,                       VK_FORMAT_UNDEFINED },
            { GraphicsFormat.R8_SRGB,                    VK_FORMAT_R8_SRGB },
            { GraphicsFormat.R8G8_SRGB,                  VK_FORMAT_R8G8_SRGB },
            { GraphicsFormat.R8G8B8_SRGB,                VK_FORMAT_R8G8B8_SRGB },
            { GraphicsFormat.R8G8B8A8_SRGB,              VK_FORMAT_R8G8B8A8_SRGB },
            { GraphicsFormat.R8_UNorm,                   VK_FORMAT_R8_UNORM },
            { GraphicsFormat.R8G8_UNorm,                 VK_FORMAT_R8G8_UNORM },
            { GraphicsFormat.R8G8B8_UNorm,               VK_FORMAT_R8G8B8_UNORM },
            { GraphicsFormat.R8G8B8A8_UNorm,             VK_FORMAT_R8G8B8A8_UNORM },
            { GraphicsFormat.R8_SNorm,                   VK_FORMAT_R8_SNORM },
            { GraphicsFormat.R8G8_SNorm,                 VK_FORMAT_R8G8_SNORM },
            { GraphicsFormat.R8G8B8_SNorm,               VK_FORMAT_R8G8B8_SNORM },
            { GraphicsFormat.R8G8B8A8_SNorm,             VK_FORMAT_R8G8B8A8_SNORM },
            { GraphicsFormat.R8_UInt,                    VK_FORMAT_R8_UINT },
            { GraphicsFormat.R8G8_UInt,                  VK_FORMAT_R8G8_UINT },
            { GraphicsFormat.R8G8B8_UInt,                VK_FORMAT_R8G8B8_UINT },
            { GraphicsFormat.R8G8B8A8_UInt,              VK_FORMAT_R8G8B8A8_UINT },
            { GraphicsFormat.R8_SInt,                    VK_FORMAT_R8_SINT },
            { GraphicsFormat.R8G8_SInt,                  VK_FORMAT_R8G8_SINT },
            { GraphicsFormat.R8G8B8_SInt,                VK_FORMAT_R8G8B8_SINT },
            { GraphicsFormat.R8G8B8A8_SInt,              VK_FORMAT_R8G8B8A8_SINT },
            { GraphicsFormat.R16_UNorm,                  VK_FORMAT_R16_UNORM },
            { GraphicsFormat.R16G16_UNorm,               VK_FORMAT_R16G16_UNORM },
            { GraphicsFormat.R16G16B16_UNorm,            VK_FORMAT_R16G16B16_UNORM },
            { GraphicsFormat.R16G16B16A16_UNorm,         VK_FORMAT_R16G16B16A16_UNORM },
            { GraphicsFormat.R16_SNorm,                  VK_FORMAT_R16_SNORM },
            { GraphicsFormat.R16G16_SNorm,               VK_FORMAT_R16G16_SNORM },
            { GraphicsFormat.R16G16B16_SNorm,            VK_FORMAT_R16G16B16_SNORM },
            { GraphicsFormat.R16G16B16A16_SNorm,         VK_FORMAT_R16G16B16A16_SNORM },
            { GraphicsFormat.R16_UInt,                   VK_FORMAT_R16_UINT },
            { GraphicsFormat.R16G16_UInt,                VK_FORMAT_R16G16_UINT },
            { GraphicsFormat.R16G16B16_UInt,             VK_FORMAT_R16G16B16_UINT },
            { GraphicsFormat.R16G16B16A16_UInt,          VK_FORMAT_R16G16B16A16_UINT },
            { GraphicsFormat.R16_SInt,                   VK_FORMAT_R16_SINT },
            { GraphicsFormat.R16G16_SInt,                VK_FORMAT_R16G16_SINT },
            { GraphicsFormat.R16G16B16_SInt,             VK_FORMAT_R16G16B16_SINT },
            { GraphicsFormat.R16G16B16A16_SInt,          VK_FORMAT_R16G16B16A16_SINT },
            { GraphicsFormat.R32_UInt,                   VK_FORMAT_R32_UINT },
            { GraphicsFormat.R32G32_UInt,                VK_FORMAT_R32G32_UINT },
            { GraphicsFormat.R32G32B32_UInt,             VK_FORMAT_R32G32B32_UINT },
            { GraphicsFormat.R32G32B32A32_UInt,          VK_FORMAT_R32G32B32A32_UINT },
            { GraphicsFormat.R32_SInt,                   VK_FORMAT_R32_SINT },
            { GraphicsFormat.R32G32_SInt,                VK_FORMAT_R32G32_SINT },
            { GraphicsFormat.R32G32B32_SInt,             VK_FORMAT_R32G32B32_SINT },
            { GraphicsFormat.R32G32B32A32_SInt,          VK_FORMAT_R32G32B32A32_SINT },
            { GraphicsFormat.R16_SFloat,                 VK_FORMAT_R16_SFLOAT },
            { GraphicsFormat.R16G16_SFloat,              VK_FORMAT_R16G16_SFLOAT },
            { GraphicsFormat.R16G16B16_SFloat,           VK_FORMAT_R16G16B16_SFLOAT },
            { GraphicsFormat.R16G16B16A16_SFloat,        VK_FORMAT_R16G16B16A16_SFLOAT },
            { GraphicsFormat.R32_SFloat,                 VK_FORMAT_R32_SFLOAT },
            { GraphicsFormat.R32G32_SFloat,              VK_FORMAT_R32G32_SFLOAT },
            { GraphicsFormat.R32G32B32_SFloat,           VK_FORMAT_R32G32B32_SFLOAT },
            { GraphicsFormat.R32G32B32A32_SFloat,        VK_FORMAT_R32G32B32A32_SFLOAT },
            { GraphicsFormat.B8G8R8_SRGB,                VK_FORMAT_B8G8R8_SRGB },
            { GraphicsFormat.B8G8R8A8_SRGB,              VK_FORMAT_B8G8R8A8_SRGB },
            { GraphicsFormat.B8G8R8_UNorm,               VK_FORMAT_B8G8R8_UNORM },
            { GraphicsFormat.B8G8R8A8_UNorm,             VK_FORMAT_B8G8R8A8_UNORM },
            { GraphicsFormat.B8G8R8_SNorm,               VK_FORMAT_B8G8R8_SNORM },
            { GraphicsFormat.B8G8R8A8_SNorm,             VK_FORMAT_B8G8R8A8_SNORM },
            { GraphicsFormat.B8G8R8_UInt,                VK_FORMAT_B8G8R8_UINT },
            { GraphicsFormat.B8G8R8A8_UInt,              VK_FORMAT_B8G8R8A8_UINT },
            { GraphicsFormat.B8G8R8_SInt,                VK_FORMAT_B8G8R8_SINT },
            { GraphicsFormat.B8G8R8A8_SInt,              VK_FORMAT_B8G8R8A8_SINT },
            { GraphicsFormat.R4G4B4A4_UNormPack16,       VK_FORMAT_R4G4B4A4_UNORM_PACK16 },
            { GraphicsFormat.B4G4R4A4_UNormPack16,       VK_FORMAT_B4G4R4A4_UNORM_PACK16 },
            { GraphicsFormat.R5G6B5_UNormPack16,         VK_FORMAT_R5G6B5_UNORM_PACK16 },
            { GraphicsFormat.B5G6R5_UNormPack16,         VK_FORMAT_B5G6R5_UNORM_PACK16 },
            { GraphicsFormat.R5G5B5A1_UNormPack16,       VK_FORMAT_R5G5B5A1_UNORM_PACK16 },
            { GraphicsFormat.B5G5R5A1_UNormPack16,       VK_FORMAT_B5G5R5A1_UNORM_PACK16 },
            { GraphicsFormat.A1R5G5B5_UNormPack16,       VK_FORMAT_A1R5G5B5_UNORM_PACK16 },
            { GraphicsFormat.E5B9G9R9_UFloatPack32,      VK_FORMAT_E5B9G9R9_UFLOAT_PACK32 },
            { GraphicsFormat.B10G11R11_UFloatPack32,     VK_FORMAT_B10G11R11_UFLOAT_PACK32 },
            { GraphicsFormat.A2B10G10R10_UNormPack32,    VK_FORMAT_A2B10G10R10_UNORM_PACK32 },
            { GraphicsFormat.A2B10G10R10_UIntPack32,     VK_FORMAT_A2B10G10R10_UINT_PACK32 },
            { GraphicsFormat.A2B10G10R10_SIntPack32,     VK_FORMAT_A2B10G10R10_SINT_PACK32 },
            { GraphicsFormat.A2R10G10B10_UNormPack32,    VK_FORMAT_A2R10G10B10_UNORM_PACK32 },
            { GraphicsFormat.A2R10G10B10_UIntPack32,     VK_FORMAT_A2R10G10B10_UINT_PACK32 },
            { GraphicsFormat.A2R10G10B10_SIntPack32,     VK_FORMAT_A2R10G10B10_SINT_PACK32 },
            { GraphicsFormat.A2R10G10B10_XRSRGBPack32,   VK_FORMAT_UNDEFINED },
            { GraphicsFormat.A2R10G10B10_XRUNormPack32,  VK_FORMAT_UNDEFINED },
            { GraphicsFormat.R10G10B10_XRSRGBPack32,     VK_FORMAT_UNDEFINED },
            { GraphicsFormat.R10G10B10_XRUNormPack32,    VK_FORMAT_UNDEFINED },
            { GraphicsFormat.A10R10G10B10_XRSRGBPack32,  VK_FORMAT_UNDEFINED },
            { GraphicsFormat.A10R10G10B10_XRUNormPack32, VK_FORMAT_UNDEFINED },
            { GraphicsFormat.D16_UNorm,                  VK_FORMAT_D16_UNORM },
            { GraphicsFormat.D24_UNorm,                  VK_FORMAT_UNDEFINED },
            { GraphicsFormat.D24_UNorm_S8_UInt,          VK_FORMAT_D24_UNORM_S8_UINT },
            { GraphicsFormat.D32_SFloat,                 VK_FORMAT_D32_SFLOAT },
            { GraphicsFormat.D32_SFloat_S8_UInt,         VK_FORMAT_D32_SFLOAT_S8_UINT },
            { GraphicsFormat.S8_UInt,                    VK_FORMAT_S8_UINT },
            { GraphicsFormat.RGBA_DXT1_SRGB,             VK_FORMAT_BC1_RGBA_SRGB_BLOCK },
            { GraphicsFormat.RGBA_DXT1_UNorm,            VK_FORMAT_BC1_RGBA_UNORM_BLOCK },
            { GraphicsFormat.RGBA_DXT3_SRGB,             VK_FORMAT_BC2_SRGB_BLOCK },
            { GraphicsFormat.RGBA_DXT3_UNorm,            VK_FORMAT_BC2_UNORM_BLOCK },
            { GraphicsFormat.RGBA_DXT5_SRGB,             VK_FORMAT_BC3_SRGB_BLOCK },
            { GraphicsFormat.RGBA_DXT5_UNorm,            VK_FORMAT_BC3_UNORM_BLOCK },
            { GraphicsFormat.R_BC4_UNorm,                VK_FORMAT_BC4_UNORM_BLOCK },
            { GraphicsFormat.R_BC4_SNorm,                VK_FORMAT_BC4_SNORM_BLOCK },
            { GraphicsFormat.RG_BC5_UNorm,               VK_FORMAT_BC5_UNORM_BLOCK },
            { GraphicsFormat.RG_BC5_SNorm,               VK_FORMAT_BC5_SNORM_BLOCK },
            { GraphicsFormat.RGB_BC6H_UFloat,            VK_FORMAT_BC6H_UFLOAT_BLOCK },
            { GraphicsFormat.RGB_BC6H_SFloat,            VK_FORMAT_BC6H_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_BC7_SRGB,              VK_FORMAT_BC7_SRGB_BLOCK },
            { GraphicsFormat.RGBA_BC7_UNorm,             VK_FORMAT_BC7_UNORM_BLOCK },
            { GraphicsFormat.RGB_ETC_UNorm,              VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK },
            { GraphicsFormat.RGB_ETC2_SRGB,              VK_FORMAT_ETC2_R8G8B8_SRGB_BLOCK },
            { GraphicsFormat.RGB_ETC2_UNorm,             VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK },
            { GraphicsFormat.RGB_A1_ETC2_SRGB,           VK_FORMAT_ETC2_R8G8B8A1_SRGB_BLOCK },
            { GraphicsFormat.RGB_A1_ETC2_UNorm,          VK_FORMAT_ETC2_R8G8B8A1_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ETC2_SRGB,             VK_FORMAT_ETC2_R8G8B8A8_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ETC2_UNorm,            VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK },
            { GraphicsFormat.R_EAC_UNorm,                VK_FORMAT_EAC_R11_UNORM_BLOCK },
            { GraphicsFormat.R_EAC_SNorm,                VK_FORMAT_EAC_R11_SNORM_BLOCK },
            { GraphicsFormat.RG_EAC_UNorm,               VK_FORMAT_EAC_R11G11_UNORM_BLOCK },
            { GraphicsFormat.RG_EAC_SNorm,               VK_FORMAT_EAC_R11G11_SNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC4X4_SRGB,          VK_FORMAT_ASTC_4x4_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC4X4_UNorm,         VK_FORMAT_ASTC_4x4_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC5X5_SRGB,          VK_FORMAT_ASTC_5x5_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC5X5_UNorm,         VK_FORMAT_ASTC_5x5_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC6X6_SRGB,          VK_FORMAT_ASTC_6x6_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC6X6_UNorm,         VK_FORMAT_ASTC_6x6_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC8X8_SRGB,          VK_FORMAT_ASTC_8x8_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC8X8_UNorm,         VK_FORMAT_ASTC_8x8_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC10X10_SRGB,        VK_FORMAT_ASTC_10x10_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC10X10_UNorm,       VK_FORMAT_ASTC_10x10_UNORM_BLOCK },
            { GraphicsFormat.RGBA_ASTC12X12_SRGB,        VK_FORMAT_ASTC_12x12_SRGB_BLOCK },
            { GraphicsFormat.RGBA_ASTC12X12_UNorm,       VK_FORMAT_ASTC_12x12_UNORM_BLOCK },
            { GraphicsFormat.YUV2,                       VK_FORMAT_UNDEFINED },
            { GraphicsFormat.RGBA_ASTC4X4_UFloat,        VK_FORMAT_ASTC_4x4_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_ASTC5X5_UFloat,        VK_FORMAT_ASTC_5x5_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_ASTC6X6_UFloat,        VK_FORMAT_ASTC_6x6_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_ASTC8X8_UFloat,        VK_FORMAT_ASTC_8x8_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_ASTC10X10_UFloat,      VK_FORMAT_ASTC_10x10_SFLOAT_BLOCK },
            { GraphicsFormat.RGBA_ASTC12X12_UFloat,      VK_FORMAT_ASTC_12x12_SFLOAT_BLOCK },
            { GraphicsFormat.D16_UNorm_S8_UInt,          VK_FORMAT_D16_UNORM_S8_UINT },
        };
    }
}