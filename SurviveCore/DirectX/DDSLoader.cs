using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local


namespace SurviveCore.DirectX {
    public static class DDSLoader {

        private const int MagicNumber = 'D' << 0 | 'D' << 8 | 'S' << 16 | ' ' << 24;
        
        public static unsafe ShaderResourceView LoadDDS(Device device, string path) {
            byte[] data = File.ReadAllBytes(path);
            Assert(data.Length >= Marshal.SizeOf<DDSHeader>() + sizeof(int));
            fixed (byte* ptr = data) {
                int offset = 0;
                Assert(To<int>(ptr) == MagicNumber);
                offset += sizeof(int);
                DDSHeader header = To<DDSHeader>(ptr + offset);
                Assert(header.Size == Marshal.SizeOf<DDSHeader>() && header.PixelFormat.Size == Marshal.SizeOf<DDSPixelFormat>());
                offset += header.Size;
                
	            int width  = header.Width;
	            int height = header.Height;
	            int depth  = header.Depth;

	            ResourceDimension resDim = ResourceDimension.Unknown;
	            int arraySize = 1;
	            Format format = Format.Unknown;
	            bool isCubeMap = false;

	            int mipCount = header.MipMapCount == 0 ? 1 : header.MipMapCount;
	            
                if (header.PixelFormat.Flags.HasFlag(PixelFormatFlags.FourCC) && header.PixelFormat.FourCC == FourCC.DX10) {
                    Assert(data.Length >= offset + Marshal.SizeOf<DDSHeader10>());
                    DDSHeader10 header10 = To<DDSHeader10>(ptr + offset);
                    offset += Marshal.SizeOf<DDSHeader10>();
	                
	                arraySize = header10.ArraySize;
	                Assert(arraySize > 0);

	                switch (header10.DXGIFormat) {
		                case Format.AI44:
			            case Format.IA44:
			            case Format.P8:
			            case Format.A8P8:
			                Assert(false);
			                break;
			            default:
				            Assert(header10.DXGIFormat.SizeOfInBits() > 0);
				            break;
	                }
	                format = header10.DXGIFormat;

	                switch (header10.ResourceDimension) {
			        	case ResourceDimension.Texture1D:
			        		Assert(!header.Flags.HasFlag(DDSFlags.Height) || height == 1);
					        height = depth = 1;
					        break;
			        	case ResourceDimension.Texture2D:
					        if (header10.MiscFlag.HasFlag(ResourceOptionFlags.TextureCube)) {
						        arraySize *= 6;
						        isCubeMap = true;
					        }
					        depth = 1;
					        break;
			        	case ResourceDimension.Texture3D:
			        		Assert(header.Flags.HasFlag(DDSFlags.Depth));
		                	Assert(arraySize == 1);
					        break;
			        	default:
			        		Assert(false);
					        break;
	                }
	                resDim = header10.ResourceDimension;

                } else {
	                format = header.PixelFormat.GetFormat();
	                Assert(format != Format.Unknown);

	                if (header.Flags.HasFlag(DDSFlags.Depth)) {
		                resDim = ResourceDimension.Texture3D;
	                }else {
		                if (header.Caps2.HasFlag(DDSCaps2.CubeMap)) {
			                Assert(header.Caps2.HasFlag(DDSCaps2.CubeMapPositiveX) &&
			                       header.Caps2.HasFlag(DDSCaps2.CubeMapNegativeX) &&
			                       header.Caps2.HasFlag(DDSCaps2.CubeMapPositiveY) &&
			                       header.Caps2.HasFlag(DDSCaps2.CubeMapNegativeY) &&
			                       header.Caps2.HasFlag(DDSCaps2.CubeMapPositiveZ) &&
			                       header.Caps2.HasFlag(DDSCaps2.CubeMapNegativeZ));
			                arraySize = 6;
			                isCubeMap = true;
		                }
		                depth = 1;
		                resDim = ResourceDimension.Texture2D;
	                }
	                Assert(format.SizeOfInBits() != 0);
                }
                
	            CheckSize(mipCount, arraySize, isCubeMap, resDim);
	            
	            //TODO AUTOGEN MIPMAPS
	            return CreateD3DResources(device, resDim, width, height, depth, mipCount, arraySize, format,
		            ResourceUsage.Immutable, BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.None, false,
		            isCubeMap,
		            FillInitData(width, height, depth, mipCount, arraySize, format, ptr + offset));

            }
            
        }
	    
	    private static Format GetFormat(this DDSPixelFormat ddpf){
			if (ddpf.Flags.HasFlag(PixelFormatFlags.RBG)){
				switch (ddpf.RGBBitCount){
					case 32:
						if (ddpf.IsBitMask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000))
							return Format.R8G8B8A8_UNorm;
						if (ddpf.IsBitMask(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000))
							return Format.B8G8R8A8_UNorm;
						if (ddpf.IsBitMask(0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000))
							return Format.B8G8R8X8_UNorm;
						if (ddpf.IsBitMask(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000))
							return Format.R10G10B10A2_UNorm;
						if (ddpf.IsBitMask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000))
							return Format.R16G16_UNorm;
						if (ddpf.IsBitMask(0xffffffff, 0x00000000, 0x00000000, 0x00000000))
							return Format.R32_Float;
						break;
					case 24:
						break;
					case 16:
						if (ddpf.IsBitMask(0x7c00, 0x03e0, 0x001f, 0x8000))
							return Format.B5G5R5A1_UNorm;
						if (ddpf.IsBitMask(0xf800, 0x07e0, 0x001f, 0x0000))
							return Format.B5G6R5_UNorm;
						if (ddpf.IsBitMask(0x0f00, 0x00f0, 0x000f, 0xf000))
							return Format.B4G4R4A4_UNorm;
						break;
				}
			} else if (ddpf.Flags.HasFlag(PixelFormatFlags.Luminance)){
				if (8 == ddpf.RGBBitCount){
					if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x00000000))
						return Format.R8_UNorm;
					if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00))
						return Format.R8G8_UNorm;
				}
				if (16 == ddpf.RGBBitCount){
					if (ddpf.IsBitMask(0x0000ffff, 0x00000000, 0x00000000, 0x00000000))
						return Format.R16_UNorm;
					if (ddpf.IsBitMask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00))
						return Format.R8G8_UNorm;
				}
			} else if (ddpf.Flags.HasFlag(PixelFormatFlags.Alpha)) {
				if (8 == ddpf.RGBBitCount)
					return Format.A8_UNorm;
			} else if (ddpf.Flags.HasFlag(PixelFormatFlags.BumpDUDV)) {
				if (16 == ddpf.RGBBitCount) {
					if (ddpf.IsBitMask(0x00ff, 0xff00, 0x0000, 0x0000))
						return Format.R8G8_SNorm;
				}
				if (32 == ddpf.RGBBitCount) {
					if (ddpf.IsBitMask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000))
						return Format.R8G8B8A8_SNorm;
					if (ddpf.IsBitMask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000))
						return Format.R16G16_SNorm;
				}
			} else if (ddpf.Flags.HasFlag(PixelFormatFlags.FourCC)) {
				if(FourCC.DXT1 == ddpf.FourCC)
					return Format.BC1_UNorm;
				if(FourCC.DXT3 == ddpf.FourCC)
					return Format.BC2_UNorm;
				if(FourCC.DXT5 == ddpf.FourCC)
					return Format.BC3_UNorm;
				if(FourCC.DXT2 == ddpf.FourCC)
					return Format.BC2_UNorm;
				if(FourCC.DXT4 == ddpf.FourCC)
					return Format.BC3_UNorm;
				if(FourCC.ATI1 == ddpf.FourCC)
					return Format.BC4_UNorm;
				if(FourCC.BC4U == ddpf.FourCC)
					return Format.BC4_UNorm;
				if(FourCC.BC4S == ddpf.FourCC)
					return Format.BC4_SNorm;
				if(FourCC.ATI2 == ddpf.FourCC)
					return Format.BC5_UNorm;
				if(FourCC.BC5U == ddpf.FourCC)
					return Format.BC5_UNorm;
				if(FourCC.BC5S == ddpf.FourCC)
					return Format.BC5_SNorm;
				if(FourCC.RGBG == ddpf.FourCC)
					return Format.R8G8_B8G8_UNorm;
				if(FourCC.GRGB == ddpf.FourCC)
					return Format.G8R8_G8B8_UNorm;
				if(FourCC.YUY2 == ddpf.FourCC)
					return Format.YUY2;
	
				switch ((int)ddpf.FourCC) {
					case 36:
						return Format.R16G16B16A16_UNorm;
					case 110:
						return Format.R16G16B16A16_SNorm;
					case 111:
						return Format.R16_Float;
					case 112:
						return Format.R16G16_Float;
					case 113:
						return Format.R16G16B16A16_Float;
					case 114:
						return Format.R32_Float;
					case 115:
						return Format.R32G32_Float;
					case 116:
						return Format.R32G32B32A32_Float;
				}
			}
			return Format.Unknown;
		}
        
	    private static Format ToSRGB(this Format format){
		    switch (format){
			    case Format.R8G8B8A8_UNorm:
				    return Format.R8G8B8A8_UNorm_SRgb;
			    case Format.BC1_UNorm:
				    return Format.BC1_UNorm_SRgb;
			    case Format.BC2_UNorm:
				    return Format.BC2_UNorm_SRgb;
			    case Format.BC3_UNorm:
				    return Format.BC3_UNorm_SRgb;
			    case Format.B8G8R8A8_UNorm:
				    return Format.B8G8R8A8_UNorm_SRgb;
			    case Format.B8G8R8X8_UNorm:
				    return Format.B8G8R8X8_UNorm_SRgb;
			    case Format.BC7_UNorm:
				    return Format.BC7_UNorm_SRgb;
			    default:
				    return format;
		    }
	    }
	    
	    private static void GetSurfaceInfo(int width, int height, Format fmt, out int outNumBytes, out int outRowBytes, out int outNumRows) {
		    int numBytes = 0;
		    int rowBytes = 0;
		    int numRows = 0;

		    bool bc = false;
		    bool packed = false;
		    bool planar = false;
		    int bpe = 0;
		    switch (fmt) {
			    case Format.BC1_Typeless:
			    case Format.BC1_UNorm:
			    case Format.BC1_UNorm_SRgb:
			    case Format.BC4_Typeless:
			    case Format.BC4_UNorm:
			    case Format.BC4_SNorm:
				    bc = true;
				    bpe = 8;
				    break;

			    case Format.BC2_Typeless:
			    case Format.BC2_UNorm:
			    case Format.BC2_UNorm_SRgb:
			    case Format.BC3_Typeless:
			    case Format.BC3_UNorm:
			    case Format.BC3_UNorm_SRgb:
			    case Format.BC5_Typeless:
			    case Format.BC5_UNorm:
			    case Format.BC5_SNorm:
			    case Format.BC6H_Typeless:
			    case Format.BC6H_Uf16:
			    case Format.BC6H_Sf16:
			    case Format.BC7_Typeless:
			    case Format.BC7_UNorm:
			    case Format.BC7_UNorm_SRgb:
				    bc = true;
				    bpe = 16;
				    break;

			    case Format.R8G8_B8G8_UNorm:
			    case Format.G8R8_G8B8_UNorm:
			    case Format.YUY2:
				    packed = true;
				    bpe = 4;
				    break;

			    case Format.Y210:
			    case Format.Y216:
				    packed = true;
				    bpe = 8;
				    break;

			    case Format.NV12:
			    case Format.Opaque420:
				    planar = true;
				    bpe = 2;
				    break;

			    case Format.P010:
			    case Format.P016:
				    planar = true;
				    bpe = 4;
				    break;
		    }

		    if (bc) {
			    int numBlocksWide = 0;
			    if (width > 0) {
				    numBlocksWide = Math.Max(1, (width + 3) / 4);
			    }

			    int numBlocksHigh = 0;
			    if (height > 0) {
				    numBlocksHigh = Math.Max(1, (height + 3) / 4);
			    }

			    rowBytes = numBlocksWide * bpe;
			    numRows = numBlocksHigh;
			    numBytes = rowBytes * numBlocksHigh;
		    }
		    else if (packed) {
			    rowBytes = ((width + 1) >> 1) * bpe;
			    numRows = height;
			    numBytes = rowBytes * height;
		    }
		    else if (fmt == Format.NV11) {
			    rowBytes = ((width + 3) >> 2) * 4;
			    numRows = height * 2; // Direct3D makes this simplifying assumption, although it is larger than the 4:1:1 data
			    numBytes = rowBytes * numRows;
		    }
		    else if (planar) {
			    rowBytes = ((width + 1) >> 1) * bpe;
			    numBytes = (rowBytes * height) + ((rowBytes * height + 1) >> 1);
			    numRows = height + ((height + 1) >> 1);
		    }
		    else {
			    int bpp = fmt.SizeOfInBits();
			    rowBytes = (width * bpp + 7) / 8; // round up to nearest byte
			    numRows = height;
			    numBytes = rowBytes * height;
		    }

			outNumBytes = numBytes;
			outRowBytes = rowBytes;
			outNumRows = numRows;
	    }

	    private static unsafe DataBox[] FillInitData(int width, int height, int depth, int mipCount, int arraySize, Format format, byte* bitData){
		    
		    DataBox[] initData = new DataBox[mipCount * arraySize];

		    int offset = 0;
		    
		    int index = 0;
		    for (int j = 0; j < arraySize; j++) {
			    int w = width;
			    int h = height;
			    int d = depth;
			    for (int i = 0; i < mipCount; i++){
				    GetSurfaceInfo(w, h, format, out int NumBytes, out int RowBytes, out int numRows);

					Assert(index < mipCount * arraySize);
					initData[index] = new DataBox((IntPtr)(bitData + offset), RowBytes, NumBytes);
					index++;
				    
					//Assert(offset + (NumBytes*d) <= bitSize);

				    offset += NumBytes * d;

				    w = w >> 1;
				    h = h >> 1;
				    d = d >> 1;
				    if (w == 0)
					    w = 1;
				    if (h == 0)
					    h = 1;
				    if (d == 0)
					    d = 1;
			    }
		    }

		    return initData;
	    }

	    private static ShaderResourceView CreateD3DResources(Device device, ResourceDimension resDim, int width, int height,
		    int depth, int mipCount, int arraySize,
		    Format format, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags,
		    ResourceOptionFlags miscFlags,
		    bool forceSRGB, bool isCubeMap, DataBox[] initData) {

		    if (forceSRGB)
			    format = format.ToSRGB();

		    switch (resDim) {
			    case ResourceDimension.Texture1D: 
			    	{
					    Texture1DDescription desc = new Texture1DDescription {
						    Width = width,
						    MipLevels = mipCount,
						    ArraySize = arraySize,
						    Format = format,
						    Usage = usage,
						    BindFlags = bindFlags,
						    CpuAccessFlags = cpuAccessFlags,
						    OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube,
					    };
					    Texture1D tex = new Texture1D(device, desc, initData);
	
					    ShaderResourceViewDescription viewdesc = new ShaderResourceViewDescription {
						    Format = format
					    };
					    if (arraySize > 1) {
						    viewdesc.Dimension = ShaderResourceViewDimension.Texture1DArray;
						    viewdesc.Texture1DArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						    viewdesc.Texture1DArray.ArraySize = arraySize;
					    }else {
						    viewdesc.Dimension = ShaderResourceViewDimension.Texture1D;
						    viewdesc.Texture1D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
					    }
					    ShaderResourceView textureView = new ShaderResourceView(device, tex, viewdesc);
					    tex.Dispose();
					    return textureView;
				    }

			    case ResourceDimension.Texture2D: 
			    	{
					    Texture2DDescription desc = new Texture2DDescription {
						    Width = width,
						    Height = height,
						    MipLevels = mipCount,
						    ArraySize = arraySize,
						    Format = format,
						    SampleDescription = new SampleDescription(1,0),
						    Usage = usage,
						    BindFlags = bindFlags,
						    CpuAccessFlags = cpuAccessFlags,
					    };
					    if (isCubeMap)
						    desc.OptionFlags = miscFlags | ResourceOptionFlags.TextureCube;
					    else
						    desc.OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube;
	
					    Texture2D tex = new Texture2D(device, desc, initData);
	
					    ShaderResourceViewDescription viewdesc = new ShaderResourceViewDescription {
						    Format = format
					    };
					    if (isCubeMap) {
						    if (arraySize > 6) {
							    viewdesc.Dimension = ShaderResourceViewDimension.TextureCubeArray;
							    viewdesc.TextureCubeArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
							    viewdesc.TextureCubeArray.CubeCount = arraySize / 6;
						    } else {
							    viewdesc.Dimension = ShaderResourceViewDimension.TextureCube;
							    viewdesc.TextureCube.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						    }
					    } else if (arraySize > 1) {
						    viewdesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
						    viewdesc.Texture2DArray.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						    viewdesc.Texture2DArray.ArraySize = arraySize;
					    } else {
						    viewdesc.Dimension = ShaderResourceViewDimension.Texture2D;
						    viewdesc.Texture2D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
					    }
					    ShaderResourceView textureView = new ShaderResourceView(device, tex, viewdesc);
					    tex.Dispose();
					    return textureView;
				    }

			    case ResourceDimension.Texture3D: 
			    	{
					    Texture3DDescription desc = new Texture3DDescription {
						    Width = width,
                	        Height = height,
                	        Depth = depth,
                	        MipLevels = mipCount,
                	        Format = format,
                	        Usage = usage,
                	        BindFlags = bindFlags,
                	        CpuAccessFlags = cpuAccessFlags,
                	        OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube
					    };
						Texture3D tex = new Texture3D(device, desc, initData);
					    
					    ShaderResourceViewDescription viewdesc = new ShaderResourceViewDescription {
						    Format = format,
						    Dimension = ShaderResourceViewDimension.Texture3D
					    };
					    viewdesc.Texture3D.MipLevels = (mipCount == 0) ? -1 : desc.MipLevels;
						ShaderResourceView textureView = new ShaderResourceView(device, tex, viewdesc);
					    tex.Dispose();
					    return textureView;
				    }
		    }
		    throw new ArgumentException();
	    }

        private static void CheckSize(int mipCount, int arraySize, bool isCubeMap, ResourceDimension resDim) {
            /*
		    if (mipCount > D3D11_REQ_MIP_LEVELS)
		{
			return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
		}

		switch (resDim)
		{
		case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
			if ((arraySize > D3D11_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION) ||
				(width > D3D11_REQ_TEXTURE1D_U_DIMENSION))
			{
				return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
			}
			break;

		case D3D11_RESOURCE_DIMENSION_TEXTURE2D:
			if (isCubeMap)
			{
				// This is the right bound because we set arraySize to (NumCubes*6) above
				if ((arraySize > D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION) ||
					(width > D3D11_REQ_TEXTURECUBE_DIMENSION) ||
					(height > D3D11_REQ_TEXTURECUBE_DIMENSION))
				{
					return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
				}
			}
			else if ((arraySize > D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION) ||
				(width > D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION) ||
				(height > D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION))
			{
				return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
			}
			break;

		case D3D11_RESOURCE_DIMENSION_TEXTURE3D:
			if ((arraySize > 1) ||
				(width > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION) ||
				(height > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION) ||
				(depth > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION))
			{
				return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
			}
			break;

		default:
			return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
		}
		     */
        }

        private static unsafe T To<T>(byte* p) where T : struct {
            return (T) Marshal.PtrToStructure((IntPtr)p, typeof(T));
        }

	    private static bool IsBitMask(this DDSPixelFormat f, uint r, uint g, uint b, uint a) {
		    return f.RBitMask == r && f.GBitMask == g && f.BBitMask == b && f.ABitMask == a;
	    }
	    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Assert(bool b) {
            if(!b)
                throw new ArgumentException("Could not parse dds file");
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DDSHeader {
            public int Size;
            public DDSFlags Flags;
            public int Height;
            public int Width;
            public int PitchOrLinearSize;
            public int Depth;
            public int MipMapCount;
            public DDSReserved Reserved;
            public DDSPixelFormat PixelFormat;
            public DDSCaps1 Caps1;
            public DDSCaps2 Caps2;
            public DDSCaps3 Caps3;
            public DDSCaps4 Caps4;
            public int Reserved2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DDSPixelFormat {
            public int Size;
            public PixelFormatFlags Flags;
            public FourCC FourCC;
            public uint RGBBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DDSReserved {
            public int Value00;
            public int Value01;
            public int Value02;
            public int Value03;
            public int Value04;
            public int Value05;
            public int Value06;
            public int Value07;
            public int Value08;
            public int Value09;
            public int Value10;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DDSHeader10 {
            public Format DXGIFormat;
            public ResourceDimension ResourceDimension;
            public ResourceOptionFlags MiscFlag;
            public int ArraySize;
            public int MiscFlags2;
        }

        [Flags]
        enum DDSFlags {
            Caps        = 0x1,
            Height      = 0x2,
            Width       = 0x4,
            Pitch       = 0x8,
            PixelFormat = 0x1000,
            MipMapCount = 0x20000,
            LinearSize  = 0x80000,
            Depth       = 0x800000
        }

        [Flags]
        enum PixelFormatFlags {
            AlphaPixels = 0x1,
            Alpha       = 0x2,
            FourCC      = 0x4,
            RBG         = 0x40,
            YUV         = 0x200,
            Luminance   = 0x2000,
	        BumpDUDV    = 0x80000
        }

        enum FourCC {
            DXT1 = 'D' << 0 | 'X' << 8 | 'T' << 16 | '1' << 24,
            DXT2 = 'D' << 0 | 'X' << 8 | 'T' << 16 | '2' << 24,
            DXT3 = 'D' << 0 | 'X' << 8 | 'T' << 16 | '3' << 24,
            DXT4 = 'D' << 0 | 'X' << 8 | 'T' << 16 | '4' << 24,
            DXT5 = 'D' << 0 | 'X' << 8 | 'T' << 16 | '5' << 24,
            DX10 = 'D' << 0 | 'X' << 8 | '1' << 16 | '0' << 24,
	        ATI1 = 'A' << 0 | 'T' << 8 | 'I' << 16 | '1' << 24,
	        BC4U = 'B' << 0 | 'C' << 8 | '4' << 16 | 'U' << 24,
	        BC4S = 'B' << 0 | 'C' << 8 | '4' << 16 | 'S' << 24,
	        ATI2 = 'A' << 0 | 'T' << 8 | 'I' << 16 | '2' << 24,
	        BC5U = 'B' << 0 | 'C' << 8 | '5' << 16 | 'U' << 24,
	        BC5S = 'B' << 0 | 'C' << 8 | '5' << 16 | 'S' << 24,
	        RGBG = 'R' << 0 | 'G' << 8 | 'B' << 16 | 'G' << 24,
	        GRGB = 'G' << 0 | 'R' << 8 | 'G' << 16 | 'B' << 24,
	        YUY2 = 'Y' << 0 | 'U' << 8 | 'Y' << 16 | '2' << 24
        }

        [Flags]
        enum DDSCaps1 {
            Complex = 0x8,
            MipMap  = 0x400000,
            Texture = 0x1000
        }

        [Flags]
        enum DDSCaps2 {
            CubeMap          = 0x200,
            CubeMapPositiveX = 0x400,
            CubeMapNegativeX = 0x800,
            CubeMapPositiveY = 0x1000,
            CubeMapNegativeY = 0x2000,
            CubeMapPositiveZ = 0x4000,
            CubeMapNegativeZ = 0x8000,
            Volume           = 0x200000
        }

        [Flags]
        enum DDSCaps3 {
            
        }

        [Flags]
        enum DDSCaps4 {
            
        }

    }
}