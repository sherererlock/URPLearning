# RT相关

```mermaid
classDiagram
class RendetTexture {
	+int width
	+int height
	+GraphicsFormat graphicsFormat
	+GraphicsFormat stencilFormat
	+GraphicsFormat depthStencilFormat
	+ bool useMipMap
	
	+ RenderBuffer colorBuffer => GetColorBuffer()
	+ RenderBuffer depthBuffer => GetDepthBuffer()
}

class RenderTargetIdentifier{
	
}

class RTHandle{
	RendetTexture rt;
	RenderTargetIdentifier nameID;
	string name
	Vector2 scaleFactor
}

class RTHandleSystem{
	
}

```

流程

```c#
UniversalRenderPipelineAsset.CreatePipeline()
{
    UniversalRenderPipeline(UniversalRenderPipelineAsset asset)
    {
        RTHandles.Initialize(Screen.width, Screen.height);
	}
    
    UniversalRenderPipelineAsset.CreateRenderers()
    {
		UniversalRenderer(UniversalRendererData data)
        {
            m_ColorBufferSystem = new RenderTargetBufferSystem("_CameraColorAttachment");
        }
    }
}


UniversalRenderPipeline.RenderSingleCameraInternal()
{
    
}

```

