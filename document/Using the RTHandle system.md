## Using the RTHandle system

### Initializing the RTHandle System

与RTHandles相关的所有操作都需要RTHandleSystem类的实例。这个类包含了分配RTHandles、释放RTHandles以及为帧设置引用大小所需的所有API。这意味着您必须在渲染管线中创建并维护一个RTHandleSystem的实例，或者利用稍后在本节中提到的静态RTHandles类。要创建自己的RTHandleSystem实例，请参阅以下代码示例：

```c#
RTHandleSystem m_RTHandleSystem = new RTHandleSystem();
m_RTHandleSystem.Initialize(Screen.width, Screen.height);
```


在初始化系统时，您必须提供起始分辨率。上述代码示例使用屏幕的宽度和高度。由于RTHandle系统仅在相机需要的分辨率大于当前最大大小时重新分配渲染纹理，内部RTHandle分辨率只能从您在此处传递的值增加。最佳实践是**将此分辨率初始化为主显示器的分辨率**。这意味着系统在应用程序开始时无需不必要地重新分配渲染纹理（并引起不希望的内存波动）。

在应用程序开始时，您只能调用一次Initialize函数。之后，您可以使用已初始化的实例来分配纹理。

由于您从相同的RTHandleSystem实例中分配了大部分RTHandles，RTHandle系统还通过RTHandles静态类提供了一个默认的全局实例。与维护自己的RTHandleSystem实例不同，这使您可以使用与实例相同的API，但无需担心实例的生命周期。使用静态实例，初始化变为如下所示：

```c#
RTHandles.Initialize(Screen.width, Screen.height);
```

The code examples in the rest of this page use the default global instance.

### Updating the RTHandle System

在使用相机进行渲染之前，您需要设置RTHandle系统用作参考大小的分辨率。为此，请调用SetReferenceSize函数。

```
RTHandles.SetReferenceSize(width, height);
```

调用此函数会产生两个效果：

1. 如果您提供的新参考大小比当前大小更大，则RTHandle系统会内部重新分配所有渲染纹理以匹配新的大小。
2. 之后，RTHandle系统会更新内部属性，设置视口和渲染纹理比例，以便在系统使用RTHandles作为活动渲染纹理时进行使用。

### Allocating and releasing RTHandles


在初始化RTHandleSystem实例之后，无论是您自己的实例还是静态的默认实例，您都可以使用它来分配RTHandles。

有三种主要的方式来分配RTHandle。它们都使用RTHandleSystem实例上的相同Alloc方法。这些函数的大多数参数与常规的Unity RenderTexture参数相同，因此有关更多信息，请参阅RenderTexture API文档。本节关注与RTHandle大小相关的参数：

- Vector2 scaleFactor: 此变体需要一个常量的2D缩放，用于宽度和高度。RTHandle系统使用这个值来计算纹理相对于**最大参考大小**的分辨率。例如，比例(1.0f, 1.0f)生成全屏纹理，而比例(0.5f, 0.5f)生成分辨率的四分之一的纹理。

- ScaleFunc scaleFunc: 对于您不想使用常量缩放来计算RTHandle大小的情况，您可以提供一个计算纹理大小的函数。该函数应该以Vector2Int作为参数，表示最大参考大小，并返回一个Vector2Int，表示您希望纹理的大小。

- int width, int height: 这是用于固定大小纹理的选项。如果您以这种方式分配纹理，它会表现得像任何常规的RenderTexture。

还有一些重载函数，可以从RenderTargetIdentifier、RenderTextures或Textures创建RTHandles。当您希望使用RTHandle API与所有纹理进行交互时，即使纹理可能并非实际的RTHandle时，这些函数会很有用。

以下代码示例包含了Alloc函数的示例用法：

```
// Simple Scale
RTHandle simpleScale = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth32, dimension: TextureDimension.Tex2D, name: "CameraDepthStencil");

// Functor
Vector2Int ComputeRTHandleSize(Vector2Int screenSize)
{
    return DoSpecificResolutionComputation(screenSize);
}

RTHandle rtHandleUsingFunctor = RTHandles.Alloc(ComputeRTHandleSize, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureDimension.Tex2D);

// Fixed size
RTHandle fixedSize = RTHandles.Alloc(256, 256, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2D);
```

当您不再需要特定的RTHandle时，您可以释放它。要做到这一点，调用Release方法。

```
myRTHandle.Release();
```

## Using RTHandles


在分配了RTHandle之后，您可以像使用常规RenderTexture一样使用它。存在到RenderTargetIdentifier和RenderTexture的隐式转换，这意味着您可以将它们与常规的相关Unity API一起使用。

然而，当您使用RTHandle系统时，RTHandle的实际分辨率可能与当前分辨率不同。例如，如果主相机以1920x1080渲染，而辅助相机以512x512渲染，即使以较低的分辨率进行渲染，所有RTHandle的分辨率都基于1920x1080分辨率。因此，在设置RTHandle作为渲染目标时要小心。CoreUtils类中有许多API可帮助您处理这个问题。例如：

```
public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
```


此函数将RTHandle设置为活动渲染目标，同时根据RTHandle的比例和当前参考大小（而不是最大大小）设置视口。

例如，当参考大小为512x512时，即使最大大小为1920x1080，比例为(1.0f, 1.0f)的纹理使用512x512的大小，因此设置了一个512x512的视口。比例为(0.5f, 0.5f)的纹理设置了一个256x256的视口，依此类推。这意味着，在使用这些辅助函数时，RTHandle系统根据RTHandle参数生成正确的视口。

这个示例只是SetRenderTarget函数的众多不同重载之一。有关所有重载的完整列表，请参阅文档。

##  Using RTHandles in shaders

在通常的方式中，在着色器中从全屏渲染纹理中采样时，UV范围覆盖整个0到1的范围。但在RTHandles中，情况并非总是如此。当前的渲染可能仅在部分视口中发生。为了考虑到这一点，在采样使用缩放的RTHandles时，必须对UV应用一个比例。在处理着色器中的RTHandles特殊性时，RTHandleSystem实例提供的RTHandleProperties结构中包含所有必要的信息。要访问它，请使用：

```
RTHandleProperties rtHandleProperties = RTHandles.rtHandleProperties;
```

This structure contains the following properties:

```
public struct RTHandleProperties
{
    public Vector2Int previousViewportSize;
    public Vector2Int previousRenderTargetSize;
    public Vector2Int currentViewportSize;
    public Vector2Int currentRenderTargetSize;
    public Vector4 rtHandleScale;
}
```


此结构提供：

1. 当前视口大小。这是您设置用于渲染的参考大小。
2. 当前渲染目标大小。这是基于最大参考大小的渲染纹理的实际大小。
3. rtHandleScale。这是应用于全屏UV以采样RTHandle的比例。

还可以获取先前帧的值。有关更多信息，请参阅相机特定的RTHandles。通常，这个结构中最重要的属性是rtHandleScale。它允许您缩放全屏UV坐标，并使用结果来采样RTHandle。例如：

```
float2 scaledUVs = fullScreenUVs * rtHandleScale.xy;
```

然而，由于部分视口始终从（0, 0）开始，当您在视口内使用整数像素坐标从纹理中加载内容时，无需重新缩放它们。

另一个重要的事项是，在将全屏四边形渲染到部分视口时，标准UV寻址机制（如wrap或clamp）没有任何好处。这是因为纹理可能比视口大。因此，在采样视口外的像素时要小心。

###  Custom SRP specific information

在SRP中，默认情况下没有提供着色器常量。因此，当您在自己的SRP中使用RTHandles时，必须自己为其着色器提供这些常量。

## Camera specific RTHandles

渲染循环使用的大多数渲染纹理可以由所有相机共享。如果它们的内容不需要在一帧之间传递，这是可以的。然而，一些渲染纹理需要持久性。一个很好的例子是在后续帧中使用主颜色缓冲区进行时域抗锯齿。这意味着相机不能与其他相机共享其RTHandle。大多数情况下，这也意味着这些RTHandles必须至少是双缓冲的（在当前帧写入，在前一帧读取）。为了解决这个问题，RTHandle系统包含了BufferedRTHandleSystems。

BufferedRTHandleSystem是一个可以多缓冲RTHandles的RTHandleSystem。其原则是通过唯一的ID标识一个缓冲区，并提供API来分配同一缓冲区的多个实例，然后从先前的帧中检索它们。这些是历史缓冲区。通常，您必须为每个相机分配一个BufferedRTHandleSystem。每个都拥有其相机特定的RTHandles。

并非每个相机都需要历史缓冲区。例如，如果一个相机不需要时域抗锯齿，您就不需要为其分配BufferedRTHandleSystem。历史缓冲区需要内存，这意味着通过不为不需要它们的相机分配历史缓冲区，您可以节省内存。另一个后果是，系统仅以目标相机的分辨率分配历史缓冲区。如果主相机是1920x1080，而另一台相机以256x256渲染并需要历史颜色缓冲区，则第二台相机仅使用256x256缓冲区，而不是1920x1080缓冲区，就像非相机特定的RTHandles一样。要创建BufferedRTHandleSystem的实例，请参阅以下代码示例：

```
BufferedRTHandleSystem  m_HistoryRTSystem = new BufferedRTHandleSystem();
```

使用BufferedRTHandleSystem分配RTHandle的过程与普通的RTHandleSystem有所不同：

```
public void AllocBuffer(int bufferId, Func<RTHandleSystem, int, RTHandle> allocator, int bufferCount);
```


bufferId是系统用于标识缓冲区的唯一ID。allocator是您提供的在需要时分配RTHandles的函数（所有实例不是预先分配的），bufferCount是请求的实例数。

然后，您可以通过ID和实例索引检索每个RTHandle，如下所示：

```
public RTHandle GetFrameRT(int bufferId, int frameIndex);
```


帧索引在零到缓冲区数量减一之间。零始终表示当前帧缓冲区，一表示前一帧缓冲区，二表示前一帧之前的缓冲区，依此类推。

要释放缓冲的RTHandle，请在BufferedRTHandleSystem上调用Release函数，并传入要释放的缓冲区的ID：

```
public void ReleaseBuffer(int bufferId);
```

与为常规RTHandleSystems提供参考大小的方式相同，您必须为每个BufferedRTHandleSystem实例提供这个参考大小。

```
public void SwapAndSetReferenceSize(int width, int height);
```

这与常规的RTHandleSystem相同，但它在内部也交换了缓冲区，以便GetFrameRT的0索引仍然引用当前帧缓冲区。处理相机特定缓冲区的这种略有不同的方式在编写着色器代码时也有影响。

使用这样的多缓冲方法，来自上一帧的RTHandles的大小可能与当前帧的不同。例如，这可能发生在动态分辨率或者甚至在编辑器中调整窗口大小时。这意味着当您访问先前帧的缓冲RTHandle时，必须相应地进行缩放。Unity用于执行此操作的比例存储在RTHandleProperties.rtHandleScale.zw中。Unity与常规RTHandles的xy值使用方式完全相同。这也是RTHandleProperties包含先前帧的视口和分辨率的原因。在处理历史缓冲区时，这可能会很有用。

## Dynamic Resolution

RTHandle系统设计的一个副产品是，您还可以使用它来模拟软件动态分辨率。由于相机的当前分辨率与实际的渲染纹理对象没有直接的关联，您可以在帧的开始时提供任何分辨率，所有渲染纹理都会相应地进行缩放。

## Reset Reference Size

有时，您可能需要在短时间内以比正常更高的分辨率进行渲染。如果您的应用程序不再需要此分辨率，则分配的额外内存将被浪费。为了避免这种情况，您可以重置RTHandleSystem的当前最大分辨率，如下所示：

```
RTHandles.ResetReferenceSize(newWidth, newHeight);
```

这会强制RTHandle系统重新分配所有RTHandles以适应新提供的大小。这是减小RTHandles大小的唯一方法。

##  RTHandle system fundamentals


这份文档描述了RTHandle（RTHandle）系统的主要原则。

RTHandle系统是建立在Unity的RenderTexture API之上的一个抽象层。它使得在使用不同分辨率的相机之间轻松重用渲染纹理变得十分简单。以下原则是RTHandle系统运作的基础：

1. 您不再需要手动分配具有固定分辨率的渲染纹理。相反，您使用与给定分辨率的全屏相关的比例来声明渲染纹理。RTHandle系统仅为整个渲染管线分配一次纹理，以便可以在不同相机之间重用。
2. 引入了参考大小的概念。这是应用程序用于渲染的分辨率。在渲染管线在特定分辨率下渲染每个相机之前，您有责任声明它。有关如何执行此操作的信息，请参阅“更新RTHandle系统”部分。
3. 在内部，RTHandle系统跟踪您声明的最大参考大小。它将其用作渲染纹理的实际大小。最大参考大小即为最大尺寸。
4. 每当声明新的用于渲染的参考大小时，RTHandle系统都会检查它是否大于当前记录的最大参考大小。如果是，RTHandle系统会在内部重新分配所有渲染纹理以适应新的大小，并用新大小替换最大参考大小。

一个实例的分配过程如下。当您分配主颜色缓冲区时，它使用比例为1，因为它是全屏纹理。您希望以屏幕分辨率渲染它。用于四分之一分辨率透明通道的缩小缓冲区会使用x轴和y轴的0.5的比例。RTHandle系统在内部使用您为渲染纹理声明的比例乘以最大参考大小来分配渲染纹理。之后，在每个相机渲染之前，您告诉系统当前参考大小是多少。基于此和所有纹理的缩放因子，RTHandle系统确定是否需要重新分配渲染纹理。如上所述，如果新的参考大小大于当前最大参考大小，则RTHandle系统会重新分配所有渲染纹理。通过这样做，RTHandle系统最终获得了所有渲染纹理的稳定最大分辨率，这很可能是主相机的分辨率。

总体来说，关键点是渲染纹理的实际分辨率不一定与当前视口相同：它可以更大。这在使用RTHandles编写渲染器时有重要影响，这也在“使用RTHandle系统”文档中有详细解释。

RTHandle系统还允许您分配具有固定大小的纹理。在这种情况下，RTHandle系统永不重新分配纹理。这使您可以一致地使用RTHandle API，无论是对RTHandle系统管理的自动调整大小的纹理，还是对您管理的常规固定大小纹理。