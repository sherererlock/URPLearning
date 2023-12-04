# Flat and Wireframe Shading

## Flat Shading

网格由三角形组成，根据定义是平面的。我们使用表面法线向量来增加曲面的错觉。这使得创建能够代表看似光滑表面的网格成为可能。然而，有时你实际上想要显示平面三角形，无论是出于风格的考虑还是为了更好地查看网格的拓扑结构。

为了使三角形看起来像它们实际上是平面的，我们必须使用实际三角形的表面法线。这将使网格呈现出具有平面阴影的外观，称为平面着色。这可以通过将三角形的三个顶点的法线向量设置为三角形的法线向量来实现。这使得在三角形之间共享顶点变得不可能，因为它们将共享法线。因此，我们最终得到更多的网格数据。如果我们能够保持顶点共享将会很方便。而且，如果我们能够对任何网格使用平面着色材质，覆盖其原始法线（如果有的话），那将会很好。

除了平面着色，显示网格的线框也可能很有用或时尚。这使得网格的拓扑结构更加明显。理想情况下，我们可以使用自定义材质，在单个通道中同时进行平面着色和线框渲染，适用于任何网格。为了创建这样的材质，我们需要一个新的着色器。我们将使用渲染系列第20部分的最终着色器作为基础。复制“My First Lighting Shader”并将其名称更改为“Flat Wireframe”

### Derivative Instructions

因为三角形是平面的，它们的表面法线在其表面的每一点上都相同。因此，为三角形渲染的每个片段应该使用相同的法线向量。但是我们目前不知道这个向量是什么。在顶点程序中，我们只能访问在网格中独立处理的顶点数据。这里存储的法线向量对我们来说没有用，除非它被设计为表示三角形的法线。而在片段程序中，我们只能访问插值的顶点法线。

为了确定表面法线，我们需要知道三角形在世界空间中的方向。这可以通过三角形顶点的位置来确定。假设三角形不是退化的，其法线向量等于三角形两条边的规范化叉积。如果它是退化的，那么它将不会被渲染。所以给定三角形的顶点a,b,c。则法线是 n=(c−a)×(b−a)，

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/flat-shading/cross-product.png)


实际上，我们并不需要使用三角形的顶点。任何位于三角形平面上的三个点都可以，只要这些点也形成一个三角形即可。具体来说，我们只需要两个位于三角形平面上的向量，只要它们不平行且大于零。

一种可能的方法是使用与渲染片段的世界位置对应的点。例如，我们当前正在渲染的片段的世界位置，其右侧片段的位置，以及其上方片段的位置，这些位置是在屏幕空间中的。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/flat-shading/fragment-positions.png)

如果我们能够访问相邻片段的世界位置，那么这种方法可能会很有效。虽然直接访问相邻片段的数据是不可能的，但我们可以访问该数据的屏幕空间导数。这是通过特殊的指令实现的，这些指令告诉我们在屏幕空间X或Y维度上，对于任何数据，片段之间的变化率是多少。

举例来说，如果我们当前片段的世界位置是 po，而在屏幕空间X维度上下一个片段的位置是 px ，那么在这两个片段之间，世界位置在X维度上的变化率为 ∂p/∂x=px−p, 这表示了在屏幕空间X维度上的世界位置的偏导数。我们可以在片段程序中使用 ddx 函数来获取这个数据，通过提供世界位置作为参数。这可以在 My Lighting.cginc 中的 InitializeFragmentNormal 函数的开始部分实现。

```
void InitializeFragmentNormal(inout Interpolators i) {
	float3 dpdx = ddx(i.worldPos);
    float3 dpdy = ddy(i.worldPos);
	i.normal = normalize(cross(dpdy, dpdx));
}
```

由于这些值代表了片段世界位置之间的差异，它们定义了三角形的两条边。实际上，我们并不知道该三角形的确切形状，但可以确保它位于原始三角形的平面内，这是唯一重要的。因此，最终的法线向量是这些向量的规范化叉积。用这个向量覆盖原始法线。

### How do `ddx` and `ddy` work?

GPU需要知道纹理坐标的屏幕空间导数，以确定在采样纹理时使用哪个mipmap级别。它通过比较相邻片段的坐标来解决这个问题。屏幕空间导数指令是这个功能的扩展，使其对于所有片段程序，对于它们使用的任何数据都是可用的。

为了能够比较片段，GPU以2×2的块处理它们。对于每个块，它确定两个X维度上的导数，对应于两个2×1片段对，以及两个Y维度上的导数，对应于两个1×2片段对。一对中的两个片段使用相同的导数数据。这意味着这些导数仅在块之间变化，每两个像素一次，而不是每个像素一次。因此，这些导数是一个近似值，当用于每个片段非线性变化的数据时，会出现块状的外观。由于三角形是平面的，这个近似值不会影响我们推导的法线向量。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/flat-shading/derivative-blocks.png)

GPU总是以2×2的块处理片段，因此沿着三角形边缘的片段可能会被处理，最终落在三角形外部。这些无效的片段会被丢弃，但仍然需要被处理以确定导数。在三角形外部，片段的插值数据会被外推到超出顶点定义的范围。

创建一个使用我们的 Flat Wireframe 着色器的新材质。任何使用这个材质的网格都应该以平面着色的方式进行渲染。它们会呈现出明显的面片效果，尽管在同时使用法线贴图时可能难以察觉。在本教程的截图中，我使用了标准的胶囊网格，并将其材质设置为灰色。

![smooth](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/flat-shading/smooth.png)

尽管这个方法有效，但实际上我们已经改变了所有依赖于“My Lighting”包含文件的着色器的行为。因此，请移除我们刚刚添加的代码。

### Geometry Shaders

有另一种方法可以确定三角形的法线。与使用导数指令不同，我们可以使用实际的三角形顶点来计算法线向量。这需要我们对每个三角形进行工作，而不是对每个单独的顶点或片段。这就是几何着色器派上用场的地方。

几何着色器阶段位于顶点和片段阶段之间。它接收顶点程序的输出，按原始图元进行分组。几何程序可以修改这些数据，然后进行插值，并用于渲染片段。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/flat-shading/shader-programs.png)

几何着色器的增值之处在于它以每个图元的形式接收顶点，对于我们的情况，是每个三角形三个顶点。网格三角形是否共享顶点并不重要，因为几何程序会输出新的顶点数据。这使我们能够推导出三角形的法线向量，并将其用作所有三个顶点的法线。

让我们将几何着色器的代码放在它自己的包含文件中，命名为 MyFlatWireframe.cginc。在这个文件中，包含 My Lighting.cginc 并定义一个 MyGeometryProgram 函数。开始时，这个函数是一个空的 void 函数。

```
#if !defined(FLAT_WIREFRAME_INCLUDED)
#define FLAT_WIREFRAME_INCLUDED

#include "My Lighting.cginc"

void MyGeometryProgram () {}

#endif
```

几何着色器仅在目标为Shader Model 4.0或更高版本时才受支持。如果定义的目标较低，Unity 将自动将目标升级到这个级别，但我们最好明确指定。要实际使用几何着色器，我们必须添加 #pragma geometry 指令，就像对顶点和片段函数一样。最后，MyFlatWireframe 必须被包含，而不是 My Lighting。将这些更改应用到 Flat Wireframe 着色器的基础、添加和延迟通道。

```
			#pragma target 4.0

			…

			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram
			#pragma geometry MyGeometryProgram

			…
			
//			#include "My Lighting.cginc"
			#include "MyFlatWireframe.cginc"
```

这将导致着色器编译错误，因为我们尚未正确定义几何函数。我们必须声明它将输出多少个顶点。这个数字可能会有变化，因此我们必须提供一个最大值。因为我们正在处理三角形，所以每次调用我们将始终输出三个顶点。通过在我们的函数上添加 maxvertexcount 属性，并将其参数设置为 3，来指定这一点。

```
[maxvertexcount(3)]
void GeometryProgram () {}
```

下一步是定义输入。由于我们正在处理插值前的顶点程序输出，因此数据类型是 InterpolatorsVertex。所以在这种情况下，类型名称在技术上并不正确，但当我们命名它时，并没有考虑到几何着色器。

```
[maxvertexcount(3)]
void MyGeometryProgram (InterpolatorsVertex i) {}
```

我们还必须声明我们正在处理哪种类型的基元，在我们的情况下是三角形。这必须在输入类型之前指定。此外，由于每个三角形都有三个顶点，因此我们正在处理一个包含三个结构的数组。我们必须明确定义这一点。

```
[maxvertexcount(3)]
void MyGeometryProgram (triangle InterpolatorsVertex i[3]) {}
```

由于几何着色器可以输出的顶点数量是可变的，我们没有一个单一的返回类型。相反，几何着色器写入一个基元流。在我们的情况下，它是一个 TriangleStream，必须被指定为一个 inout 参数

```
[maxvertexcount(3)]
void MyGeometryProgram (
	triangle InterpolatorsVertex i[3],
	inout TriangleStream stream
) {}
```

TriangleStream 的工作方式类似于 C# 中的泛型类型。它需要知道我们将要提供给它的顶点数据的类型，这仍然是 InterpolatorsVertex。

```
[maxvertexcount(3)]
void MyGeometryProgram (
	triangle InterpolatorsVertex i[3],
	inout TriangleStream<InterpolatorsVertex> stream
) {}
```

现在函数签名是正确的，我们必须将顶点数据放入流中。这是通过按照接收到它们的顺序，对流的 Append 函数进行每个顶点一次的调用来完成的。

```
[maxvertexcount(3)]
void MyGeometryProgram (
	triangle InterpolatorsVertex i[3],
	inout TriangleStream<InterpolatorsVertex> stream
) {
	stream.Append(i[0]);
	stream.Append(i[1]);
	stream.Append(i[2]);
}
```

到目前为止，我们的着色器再次起作用。我们添加了一个自定义的几何阶段，它简单地将顶点程序的输出传递，没有进行修改。

### Modifying Vertex Normals Per Triangle

To find the triangle's normal vector, begin by extracting the world positions of its three vertices.

```
	float3 p0 = i[0].worldPos.xyz;
	float3 p1 = i[1].worldPos.xyz;
	float3 p2 = i[2].worldPos.xyz;
	
	stream.Append(i[0]);
	stream.Append(i[1]);
	stream.Append(i[2]);
```

Now we can perform the normalized cross product, once per triangle.

```
	float3 p0 = i[0].worldPos.xyz;
	float3 p1 = i[1].worldPos.xyz;
	float3 p2 = i[2].worldPos.xyz;

	float3 triangleNormal = normalize(cross(p1 - p0, p2 - p0));
```

Replace the vertex normals with this triangle normal.

```
	float3 triangleNormal = normalize(cross(p1 - p0, p2 - p0));
	i[0].normal = triangleNormal;
	i[1].normal = triangleNormal;
	i[2].normal = triangleNormal;
```

### Which approach is best

如果你只需要平面着色，屏幕空间导数是实现这种效果的最经济的方式。然后，你还可以从网格数据中剥离法线—Unity可以自动完成这项工作—并且还可以移除法线插值器数据。一般来说，如果可以不使用自定义的几何阶段，那就不要使用。尽管如此，我们将继续使用几何方法，因为我们将需要它来进行线框渲染。

## Rendering the Wireframe

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/wire-effect.png)

在处理平面着色之后，我们继续渲染网格的线框。我们不会创建新的几何形状，也不会使用额外的传递来绘制线条。我们将通过在三角形内部沿着它们的边缘添加线条效果来创建线框视觉效果。这可以创建一个令人信服的线框，尽管定义形状轮廓的线条将显得比内部的线条要薄一半。这通常不太容易注意到，因此我们将接受这种不一致性。

### Barycentric Coordinates

要在三角形边缘添加线条效果，我们需要知道片段到最近边缘的距离。这意味着三角形的拓扑信息需要在片段程序中可用。这可以通过将三角形的重心坐标添加到插值数据中来实现。

将重心坐标添加到三角形的一种方法是使用网格的顶点颜色来存储它们。每个三角形的第一个顶点变为红色，第二个变为绿色，第三个变为蓝色。然而，这将要求以这种方式分配顶点颜色的网格，并且无法共享顶点。我们希望找到适用于任何网格的解决方案。幸运的是，我们可以使用几何程序来添加所需的坐标。

由于网格未提供重心坐标，顶点程序不知道它们。因此，它们不是 InterpolatorsVertex 结构的一部分。为了让几何程序输出它们，我们必须定义一个新的结构。首先，在 MyGeometryProgram 之上定义 InterpolatorsGeometry。它应该包含与 InterpolatorsVertex 相同的数据，因此将其用作其内容。

```
struct InterpolatorsGeometry {
	InterpolatorsVertex data;
};
```

调整 MyGeometryProgram 的流数据类型，使其使用新的结构。在函数内部定义这种类型的变量，将输入数据分配给它们，并将它们附加到流中，而不是直接通过将输入传递。

```
void MyGeometryProgram (
	triangle InterpolatorsVertex i[3],
	inout TriangleStream<InterpolatorsGeometry> stream
) {
	…

	InterpolatorsGeometry g0, g1, g2;
	g0.data = i[0];
	g1.data = i[1];
	g2.data = i[2];

	stream.Append(g0);
	stream.Append(g1);
	stream.Append(g2);
}
```

现在我们可以向 InterpolatorsGeometry 添加附加数据。为其添加一个 float3 类型的 barycentricCoordinators 向量，使用第十个插值器语义。

```
struct InterpolatorsGeometry {
	InterpolatorsVertex data;
	float3 barycentricCoordinates : TEXCOORD9;
};
```

为每个顶点分配一个重心坐标。顶点获得哪个坐标并不重要，只要它们是有效的。

```
	g0.barycentricCoordinates = float3(1, 0, 0);
	g1.barycentricCoordinates = float3(0, 1, 0);
	g2.barycentricCoordinates = float3(0, 0, 1);

	stream.Append(g0);
	stream.Append(g1);
	stream.Append(g2);
```

请注意，重心坐标始终加起来等于1。因此，我们可以只传递两个坐标，通过从1中减去其他两个来得到第三个坐标。这意味着我们只需要插值一个数字，所以让我们进行这个更改。

```
struct InterpolatorsGeometry {
	InterpolatorsVertex data;
	float2 barycentricCoordinates : TEXCOORD9;
};
	
	[maxvertexcount(3)]
void MyGeometryProgram (
	triangle InterpolatorsVertex i[3],
	inout TriangleStream<InterpolatorsGeometry> stream
) {
	…

	g0.barycentricCoordinates = float2(1, 0);
	g1.barycentricCoordinates = float2(0, 1);
	g2.barycentricCoordinates = float2(0, 0);

	…
}
```

Are our barycentric coordinates now interpolated, with barycentric coordinates?

很遗憾，我们不能直接使用用于插值顶点数据的重心坐标。在最终到达顶点程序之前，GPU 可以决定在各种原因下将三角形分割为更小的三角形。因此，GPU 用于最终插值的坐标可能与预期的不同。

### Defining Extra Interpolators

在这一点上，我们将重心坐标传递给片段程序，但它还不知道这些坐标。我们必须将它们添加到“My Lighting”中的 Interpolators 定义中。但我们不能简单地假设这些数据是可用的。这只适用于我们的 Flat Wireframe 着色器。因此，让我们使任何使用“My Lighting”的人都能通过几何着色器定义自己的插值器数据，通过定义一个 CUSTOM_GEOMETRY_INTERPOLATORS 宏来实现。为了支持这一点，在 Interpolators 中插入该宏，如果在那一点已经定义了的话。

```
struct Interpolators {
	…

	#if defined (CUSTOM_GEOMETRY_INTERPOLATORS)
		CUSTOM_GEOMETRY_INTERPOLATORS
	#endif
};
```

现在我们可以在 MyFlatWireframe 中定义这个宏。我们必须在包含 My Lighting 之前执行这个操作。我们还可以在 InterpolatorsGeometry 中使用它，这样我们只需要编写一次代码。

```
#define CUSTOM_GEOMETRY_INTERPOLATORS \
	float2 barycentricCoordinates : TEXCOORD9;

#include "My Lighting.cginc"

struct InterpolatorsGeometry {
	InterpolatorsVertex data;
//	float2 barycentricCoordinates : TEXCOORD9;
	CUSTOM_GEOMETRY_INTERPOLATORS
};
```

Why am I getting a conversion compile error?

如果您正在使用 Rendering 20 中的软件包，那是因为教程中存在一个错误。My Lighting 中的 ComputeVertexLightColor 函数应该使用 InterpolatorsVertex 作为其参数类型，但错误地使用了 Interpolators。修复这个错误，错误就会消失。如果您正在使用自己的代码，可能会出现类似的错误，其中某处使用了错误的插值器结构类型。

### Splitting My Lighting

我们将如何使用重心坐标来可视化线框呢？无论我们如何做，都不应涉及到 My Lighting。相反，我们可以通过在其代码中插入我们自己的函数，使其功能可以通过另一个文件进行重新连接。

要覆盖 My Lighting 的功能，我们必须在包含该文件之前定义新代码。但为了做到这一点，我们需要访问插值器，而插值器是在 My Lighting 中定义的，因此我们首先必须包含它。为了解决这个问题，我们需要将 My Lighting 拆分为两个文件。复制 My Lighting 开头的代码，包括包含语句、插值器结构和所有 Get 函数。将这段代码放入一个新的 My Lighting Input.cginc 文件中。为该文件创建自己的包含保护宏定义 MY_LIGHTING_INPUT_INCLUDED。

```
#if !defined(MY_LIGHTING_INPUT_INCLUDED)
#define MY_LIGHTING_INPUT_INCLUDED

#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	#if !defined(FOG_DISTANCE)
		#define FOG_DEPTH 1
	#endif
	#define FOG_ON 1
#endif

…

float3 GetEmission (Interpolators i) {
	#if defined(FORWARD_BASE_PASS) || defined(DEFERRED_PASS)
		#if defined(_EMISSION_MAP)
			return tex2D(_EmissionMap, i.uv.xy) * _Emission;
		#else
			return _Emission;
		#endif
	#else
		return 0;
	#endif
}

#endif
```

删除 My Lighting 中相同的代码。为了使现有的着色器继续工作，改为包含 My Lighting Input。

```
#if !defined(MY_LIGHTING_INCLUDED)
#define MY_LIGHTING_INCLUDED

//#include "UnityPBSLighting.cginc"
// …
//
//float3 GetEmission (Interpolators i) {
//	…
//}

#include "My Lighting Input.cginc"

void ComputeVertexLightColor (inout InterpolatorsVertex i) {
	#if defined(VERTEXLIGHT_ON)
		i.vertexLightColor = Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, i.worldPos.xyz, i.normal
		);
	#endif
}
```

现在可以在包含 My Lighting 之前包含 My Lighting Input。其包含保护宏将确保防止重复包含。在 MyFlatWireframe 中执行这样的操作。

```
#include "My Lighting Input.cginc"

#include "My Lighting.cginc"
```

### Rewiring Albedo

让我们通过调整材质的反照率来添加线框效果。这要求我们替换 My Lighting 的默认反照率函数。与自定义几何插值器类似，我们将通过一个宏 ALBEDO_FUNCTION 来实现这一点。在 My Lighting 中，在确保已经包含输入后，检查该宏是否已定义。如果没有定义，将其定义为 GetAlbedo 函数，使其成为默认值。

```
#include "My Lighting Input.cginc"

#if !defined(ALBEDO_FUNCTION)
	#define ALBEDO_FUNCTION GetAlbedo
#endif
```

In the `MyFragmentProgram` function, replace the invocation of `GetAlbedo` with the macro.

```
	float3 albedo = DiffuseAndSpecularFromMetallic(
		ALBEDO_FUNCTION(i), GetMetallic(i), specularTint, oneMinusReflectivity
	);
```

现在我们可以在包含 My Lighting Input 后在 MyFlatWireframe 中创建我们自己的反照率函数。它需要与原始的 GetAlbedo 函数具有相同的形式。首先，简单地通过原始函数的结果。之后，使用我们自己函数的名称定义 ALBEDO_FUNCTION 宏，然后包含 My Lighting。

```
#include "My Lighting Input.cginc"

float3 GetAlbedoWithWireframe (Interpolators i) {
	float3 albedo = GetAlbedo(i);
	return albedo;
}

#define ALBEDO_FUNCTION GetAlbedoWithWireframe

#include "My Lighting.cginc"
```

To verify that we have indeed control over the fragment's albedo, use the barycentric coordinates directly as the albedo.

```
float3 GetAlbedoWithWireframe (Interpolators i) {
	float3 albedo = GetAlbedo(i);
	float3 barys;
	barys.xy = i.barycentricCoordinates;
	barys.z = 1 - barys.x - barys.y;
	albedo = barys;
	return albedo;
}
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/barycentric-albedo.png)

### Creating Wires

为了创建线框效果，我们需要知道片段距离最近的三角形边有多近。我们可以通过取重心坐标的最小值来找到这个距离。这给我们在重心域中到边的最小距离。让我们直接将其用作反照率。

```
	float3 albedo = GetAlbedo(i);
	float3 barys;
	barys.xy = i.barycentricCoordinates;
	barys.z = 1 - barys.x - barys.y;
//	albedo = barys;
	float minBary = min(barys.x, min(barys.y, barys.z));
	return albedo * minBary;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/minimum-coordinate.png)

这看起来有点像在白色网格上方的黑色线框，但模糊度太高。这是因为到最近边的距离从边缘为零到三角形中心为 1/3。为了使其看起来更像细线，我们必须更快地过渡到白色，例如在 0 和 0.1 之间过渡从黑色到白色。为了使过渡平滑，让我们使用 smoothstep 函数。

What's the `smoothstep` function?

是的，smoothstep 是一个标准函数，**它在两个值之间产生平滑的曲线过渡**，而不是线性插值。它的定义是**3t^2 - 2t^3**，其中 t 的取值范围是从 0 到 1。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/smoothstep.png)

smoothstep 函数有三个参数，a、b 和 c。前两个参数 a 和 b 定义过渡应覆盖的范围，而 c 是要平滑的值。这导致 t = (c - a) / (b - a)，在使用之前将其夹紧到 0–1 范围。

```
	float minBary = min(barys.x, min(barys.y, barys.z));
	minBary = smoothstep(0, 0.1, minBary);
	return albedo * minBary;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/adjusted-transition.png)

### Fixed Wire Width

线框效果开始看起来不错，但仅适用于边缘长度大致相同的三角形。而且，由于线框是三角形的一部分，它们受到视距的影响。理想情况下，线条具有固定的可视厚度。

为了在屏幕空间保持线框的厚度恒定，我们必须调整用于 smoothstep 函数的范围。范围取决于测量到的到边缘的距离在视觉上的变化速度。我们可以使用屏幕空间导数指令来找出这一点。

变化速率在屏幕空间维度上可能是不同的。我们应该使用哪个？我们可以同时使用两者，简单地将它们相加。此外，由于变化可能是正数或负数，我们应该使用它们的绝对值。通过直接使用结果作为范围，我们最终得到大致覆盖两个片段的线条。

```
	float minBary = min(barys.x, min(barys.y, barys.z));
	float delta = abs(ddx(minBary)) + abs(ddy(minBary));
	minBary = smoothstep(0, delta, minBary);
```

This formula is also available as the convenient `fwidth` function, so let's use that.

```
	float delta = fwidth(minBary);
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/fixed-width.png)

由于最终的线可能显得有点太细，我们可以通过将过渡略微移离边缘来修复这个问题，例如使用与混合范围相同的值。

```
	minBary = smoothstep(delta, 2 * delta, minBary);
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/thicker-width-artifacts.png)

这产生了更清晰的线条，但也在三角形角附近的线条中显露出锯齿状伪影。这些伪影之所以出现是因为在这些区域最近的边缘突然变化，导致了不连续的导数。为了解决这个问题，我们必须使用各个重心坐标的导数，分别混合它们，然后在此之后取最小值。

```
	barys.z = 1 - barys.x - barys.y;
	float3 deltas = fwidth(barys);
	barys = smoothstep(deltas, 2 * deltas, barys);
	float minBary = min(barys.x, min(barys.y, barys.z));
//	float delta = fwidth(minBary);
//	minBary = smoothstep(delta, 2 * delta, minBary);
	return albedo * minBary;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/thicker-width.png)

### Configurable Wires

我们有一个功能正常的线框效果，但您可能想要使用不同的线条厚度、混合区域或颜色。也许您想要为每种材质使用不同的设置。因此，让我们将其配置化。为此，在 Flat Wireframe 着色器中添加三个属性。第一个是线框颜色，默认为黑色。第二个是线框平滑度，控制过渡范围。范围从零到十应该足够了，其中默认为一，表示 fwidth 测量的倍数。第三个是线框厚度，与平滑度相同的设置。

```
		_WireframeColor ("Wireframe Color", Color) = (0, 0, 0)
		_WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 1
		_WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 1
```

添加相应的变量到 MyFlatWireframe 中，并在 GetAlbedoWithWireframe 中使用它们。通过根据平滑的最小值在线框颜色和原始反照率之间进行插值，确定最终的反照率。

```
float3 _WireframeColor;
float _WireframeSmoothing;
float _WireframeThickness;

float3 GetAlbedoWithWireframe (Interpolators i) {
	float3 albedo = GetAlbedo(i);
	float3 barys;
	barys.xy = i.barycentricCoordinates;
	barys.z = 1 - barys.x - barys.y;
	float3 deltas = fwidth(barys);
	float3 smoothing = deltas * _WireframeSmoothing;
	float3 thickness = deltas * _WireframeThickness;
	barys = smoothstep(thickness, thickness + smoothing, barys);
	float minBary = min(barys.x, min(barys.y, barys.z));
//	return albedo * minBary;
	return lerp(_WireframeColor, albedo, minBary);
}
```

虽然着色器现在是可配置的，但属性还没有出现在我们的自定义着色器 GUI 中。我们可以为 Flat Wireframe 创建一个新的 GUI，但让我们采取捷径，直接将属性添加到 MyLightingShaderGUI 中。给它一个新的 DoWireframe 方法，用于创建线框的小节。

```
	void DoWireframe () {
		GUILayout.Label("Wireframe", EditorStyles.boldLabel);
		EditorGUI.indentLevel += 2;
		editor.ShaderProperty(
			FindProperty("_WireframeColor"),
			MakeLabel("Color")
		);
		editor.ShaderProperty(
			FindProperty("_WireframeSmoothing"),
			MakeLabel("Smoothing", "In screen space.")
		);
		editor.ShaderProperty(
			FindProperty("_WireframeThickness"),
			MakeLabel("Thickness", "In screen space.")
		);
		EditorGUI.indentLevel -= 2;
	}
```

为了使 MyLightingShaderGUI 支持具有和不具有线框的着色器，只有在着色器具有 _WireframeColor 属性时，在其 OnGUI 方法中调用 DoWireframe。我们简单地假设如果该属性可用，那么它就包含所有三个属性。

```
	public override void OnGUI (
		MaterialEditor editor, MaterialProperty[] properties
	) {
		this.target = editor.target as Material;
		this.editor = editor;
		this.properties = properties;
		DoRenderingMode();
		if (target.HasProperty("_WireframeColor")) {
			DoWireframe();
		}
		DoMain();
		DoSecondary();
		DoAdvanced();
	}
```

![inspector](https://catlikecoding.com/unity/tutorials/advanced-rendering/flat-and-wireframe-shading/rendering-the-wireframe/inspector.png)

现在您可以渲染具有平面着色和可配置线框的网格了。这将在下一个高级渲染教程“Tessellation（镶嵌）”中派上用场。