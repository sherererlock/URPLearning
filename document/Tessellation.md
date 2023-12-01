# Tessellation

## Hulls and Domains

Tessellation是将事物切割成更小部分的艺术。在我们的情况下，我们将对三角形进行细分，以便得到覆盖相同空间的更小三角形。这使得可以向几何图形添加更多细节，尽管在本教程中我们将专注于细分过程本身。

GPU能够分割传递给它进行渲染的三角形。它出于各种原因执行此操作，例如当三角形的一部分最终被剪切时。我们无法控制这一点，但还有一个我们被允许配置的细分阶段。该阶段位于顶点着色器和片段着色器阶段之间。但这并不像只是向我们的着色器添加另一个程序那么简单。我们将需要一个外壳程序和域程序。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/hulls-and-domains/shader-programs.png)

### Creating a Tessellation Shader

第一步是创建一个启用了细分的着色器。让我们将我们需要的代码放在一个名为MyTessellation.cginc的文件中，并添加一个专属的包含保护。

```
#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

#endif
```

为了清楚地看到三角形被细分，我们将使用Flat Wireframe Shader。复制该着色器，将其重命名为Tessellation Shader，并调整其菜单名称。

```
Shader "Custom/Tessellation" { … }
```

在使用细分时，着色器的最低目标级别是4.6。如果我们不手动设置这个，Unity会发出警告并自动使用该级别。我们将在前向基础通道、附加通道以及延迟通道中添加细分阶段。在这些通道中，将MyFlatWireframe之后，包含MyTessellation。

```
			#pragma target 4.6
						
			…

			#include "MyFlatWireframe.cginc"
			#include "MyTessellation.cginc"
```

创建一个依赖于这个着色器的材质，并在场景中添加一个使用它的四边形。我将材质设为灰色，这样它就不会像Flat Wireframe材质那样太亮。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/hulls-and-domains/quad.png)

我们将使用这个四边形来测试我们的细分着色器。请注意，它由两个等腰直角三角形组成。短边的长度为1，而长对角边的长度为√2。

### Hull Shaders

与几何着色器一样，细分阶段是灵活的，可以处理三角形、四边形或隔线。我们必须告诉它要处理哪种表面，并提供必要的数据。这是外壳程序的工作。在MyTessellation中添加一个程序，从一个什么也不做的void函数开始。

```
void MyHullProgram () {}
```

外壳程序在一个表面片上操作，该片作为参数传递给它。我们必须添加一个InputPatch参数以实现这一点。

```
void MyHullProgram (InputPatch patch) {}
```

一个patch是一组网格顶点。就像我们为几何函数的stream参数所做的那样，我们必须指定顶点的数据格式。目前我们将使用VertexData结构。

```
void MyHullProgram (InputPatch<VertexData> patch) {}
```

因为我们正在处理三角形，所以每个patch将包含三个顶点。这个数量必须作为InputPatch的第二个模板参数来指定。

```
void MyHullProgram (InputPatch<VertexData, 3> patch) {}
```

外壳程序的工作是将所需的顶点数据传递给细分阶段。尽管它被馈送整个patch，但该函数每次应仅输出一个顶点。它将针对patch中的每个顶点调用一次，并带有一个额外的参数，指定它应该使用哪个控制点（顶点）。该参数是带有SV_OutputControlPointID语义的无符号整数。

```
void MyHullProgram (
	InputPatch<VertexData, 3> patch,
	uint id : SV_OutputControlPointID
) {}
```

简单地通过将patch视为数组进行索引，并返回所需的元素。

```
VertexData MyHullProgram (
	InputPatch<VertexData, 3> patch,
	uint id : SV_OutputControlPointID
) {
	return patch[id];
}
```

这看起来像一个功能性的程序，所以让我们添加一个编译器指令以将其用作外壳着色器。对所有涉及的三个着色器通道都进行这样的操作。

```
			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram
			#pragma hull MyHullProgram
			#pragma geometry MyGeometryProgram
```

这将产生一些编译错误，抱怨我们没有正确配置外壳着色器。与几何函数一样，它需要属性来配置它。首先，我们必须明确告诉它正在处理三角形。这通过UNITY_domain属性完成，参数是tri。

```
[UNITY_domain("tri")]
VertexData MyHullProgram …
```

这还不够。我们还必须明确指定我们正在每个patch输出三个控制点，分别对应三角形的每个角

```
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
VertexData MyHullProgram …
```

当GPU创建新三角形时，它需要知道我们是希望它们按顺时针还是逆时针定义。与Unity中的所有其他三角形一样，它们应该是顺时针的。这通过UNITY_outputtopology属性进行控制，其参数应为triangle_cw。

```
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
VertexData MyHullProgram …
```

GPU还需要通过UNITY_partitioning属性告诉它应该如何切割patch。有几种不同的分割方法，我们稍后会调查。目前，只需使用整数模式。

```
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
VertexData MyHullProgram …
```

除了分割方法之外，GPU还必须知道patch应该被切割成多少部分。这不是一个常量值，它可能因patch而异。我们必须提供一个用于评估这一点的函数，称为patch常量函数。让我们假设我们有这样一个函数，名为MyPatchConstantFunction。

```
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("MyPatchConstantFunction")]
VertexData MyHullProgram …
```

### Patch Constant Functions

如何对patch进行细分是patch的一个属性。这意味着patch常量函数仅对每个patch调用一次，而不是对每个控制点调用一次。这就是为什么它被称为常量函数，它在整个patch中都是常量的。实际上，这个函数是与MyHullProgram并行运行的一个子阶段。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/hulls-and-domains/hull-shader.png)

为了确定如何对三角形进行细分，GPU使用四个细分因子。三角形patch的每条边都有一个因子。还有一个用于三角形内部的因子。这三个边向量必须作为具有SV_TessFactor语义的float数组传递。内部因子使用SV_InsideTessFactor语义。让我们为此创建一个结构体。

```
struct TessellationFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};
```

patch常量函数以patch作为输入参数，并输出细分因子。让我们现在创建这个缺失的函数。简单地将所有因子设置为1。这将指示细分阶段不对patch进行细分。

```
TessellationFactors MyPatchConstantFunction (InputPatch<VertexData, 3> patch) {
	TessellationFactors f;
    f.edge[0] = 1;
    f.edge[1] = 1;
    f.edge[2] = 1;
	f.inside = 1;
	return f;
}
```

### Domain Shaders

在这一点上，着色器编译器将抱怨没有细分评估着色器的着色器无法存在。外壳着色器只是让细分工作正常所需的部分之一。一旦细分阶段确定了如何对patch进行细分，就由几何着色器来评估结果并生成最终三角形的顶点。因此，让我们为我们的域着色器创建一个函数，再次从一个存根开始。

```
void MyDomainProgram () {}
```

外壳着色器和域着色器都作用于相同的域，即三角形。我们再次通过UNITY_domain属性来表示这一点。

```
[UNITY_domain("tri")]
void MyDomainProgram () {}
```

域程序接收用于细分的细分因子，以及原始patch，该patch在这种情况下是OutputPatch类型。

```
[UNITY_domain("tri")]
void MyDomainProgram (
	TessellationFactors factors,
	OutputPatch<VertexData, 3> patch
) {}
```

虽然细分阶段确定了patch应该如何细分，但它并不生成任何新的顶点。相反，它为这些顶点提供了重心坐标。域着色器负责使用这些坐标推导出最终的顶点。为了使这成为可能，域函数对每个顶点调用一次，并为其提供了其重心坐标，其具有SV_DomainLocation语义。

```
[UNITY_domain("tri")]
void MyDomainProgram (
	TessellationFactors factors,
	OutputPatch<VertexData, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {}
```

在函数内部，我们必须生成最终的顶点数据。

```
[UNITY_domain("tri")]
void MyDomainProgram (
	TessellationFactors factors,
	OutputPatch<VertexData, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {
	VertexData data;
}
```

要找到这个顶点的位置，我们必须在原始三角形域上进行插值，使用重心坐标。X、Y和Z坐标确定第一个、第二个和第三个控制点的权重。

```
	VertexData data;
	data.vertex =
		patch[0].vertex * barycentricCoordinates.x +
		patch[1].vertex * barycentricCoordinates.y +
		patch[2].vertex * barycentricCoordinates.z;
```

我们必须以相同的方式插值所有顶点数据。让我们为此定义一个方便的宏，它可以用于所有矢量大小。

```
//	data.vertex =
//		patch[0].vertex * barycentricCoordinates.x +
//		patch[1].vertex * barycentricCoordinates.y +
//		patch[2].vertex * barycentricCoordinates.z;
	
	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
```

除了位置之外，还要插值法线、切线和所有UV坐标。

```
	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv1)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv2)
```

唯一不进行插值的是实例ID。由于Unity不支持同时进行GPU实例化和细分，复制此ID没有意义。为了防止编译错误，从三个着色器通道中移除multi-compile指令。这也将从着色器的GUI中移除实例化选项。

```
//			#pragma multi_compile_instancing
//			#pragma instancing_options lodfade force_same_maxcount_for_gl
```

现在我们有了一个新的顶点，它将在此阶段之后发送到几何程序或插值器。但是这些程序期望的是InterpolatorsVertex数据，而不是VertexData。为了解决这个问题，我们让域着色器接管原始顶点程序的职责。这通过在其中调用MyVertexProgram（就像调用任何其他函数一样）并返回其结果来完成。

```
[UNITY_domain("tri")]
InterpolatorsVertex MyDomainProgram (
	TessellationFactors factors,
	OutputPatch<VertexData, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {
	…
	
	return MyVertexProgram(data);
}
```

现在我们可以将域着色器添加到我们的三个着色器通道中，但仍然会出现错误。

```
			#pragma hull MyHullProgram
			#pragma domain MyDomainProgram
```

### Control Points

MyVertexProgram只需要在某个时候调用一次，只是我们改变了这个调用发生的地方。但是我们仍然必须指定一个在顶点着色器阶段期间调用的顶点程序，该阶段位于外壳着色器之前。在那一点上我们不必做任何事情，所以我们可以使用一个简单地将顶点数据不加修改地传递的函数。

```
VertexData MyTessellationVertexProgram (VertexData v) {
	return v;
}
```

让我们的三个着色器通道从现在开始使用这个函数作为它们的顶点程序。

```
#pragma vertex MyTessellationVertexProgram
```

这将产生另一个编译错误，抱怨位置语义的重复使用。为了使这个工作，我们必须为我们的顶点程序使用一个替代的输出结构，该结构使用INTERNALTESSPOS语义作为顶点位置。结构的其余部分与VertexData相同，只是它从不具有实例ID。由于此顶点数据用作细分过程的控制点，让我们将其命名为TessellationControlPoint。

```
struct TessellationControlPoint {
	float4 vertex : INTERNALTESSPOS;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
};
```

更改MyTessellationVertexProgram，使其将顶点数据放入控制点结构中并返回该结构。

```
TessellationControlPoint MyTessellationVertexProgram (VertexData v) {
	TessellationControlPoint p;
	p.vertex = v.vertex;
	p.normal = v.normal;
	p.tangent = v.tangent;
	p.uv = v.uv;
	p.uv1 = v.uv1;
	p.uv2 = v.uv2;
	return p;
}
```

接下来，MyHullProgram也必须更改，以便使用TessellationControlPoint而不是VertexData。只需更改其参数类型。

```
TessellationControlPoint MyHullProgram (
	InputPatch<TessellationControlPoint, 3> patch,
	uint id : SV_OutputControlPointID
) {
	return patch[id];
}
```

The same goes for the patch constant function.

```
TessellationFactors MyPatchConstantFunction (
	InputPatch<TessellationControlPoint, 3> patch
) {
	…
}
```

And the domain program's parameter type has to change as well.

```
InterpolatorsVertex MyDomainProgram (
	TessellationFactors factors,
	OutputPatch<TessellationControlPoint, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {
	…
}
```

在这一点上，我们终于有了一个正确的细分着色器。它应该像之前一样编译和渲染四边形。它尚未被细分，因为细分因子始终为1。

## Subdividing Triangles

整个细分设置的目的是我们可以细分patch。这允许我们用一组较小的三角形替换单个三角形。我们现在要做的就是这个。

### Tessellation Factors

三角形patch如何被细分是由其细分因子控制的。我们在MyPatchConstantFunction中确定这些因子。目前，我们将它们全部设置为1，这不会产生视觉变化。外壳、细分和域着色器阶段正在工作，但它们传递原始顶点数据并没有生成任何新的内容。为了改变这一点，将所有因子设置为2。

```
TessellationFactors MyPatchConstantFunction (
	InputPatch<TessellationControlPoint, 3> patch
) {
	TessellationFactors f;
    f.edge[0] = 2;
    f.edge[1] = 2;
    f.edge[2] = 2;
	f.inside = 2;
	return f;
}
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/factors-2.png)

现在三角形确实被细分了。它们的所有边都被分成了两个子边，每个三角形有三个新的顶点。此外，每个三角形的中心还添加了一个额外的顶点。这使得每条原始边可以生成两个新的三角形，因此每个原始三角形都被六个较小的三角形替换。由于四边形由两个三角形组成，所以现在总共有十二个三角形。

如果将所有因子都设置为3，每个边将被分成三个子边。在这种情况下，将没有中心顶点。相反，在原始三角形内部添加了三个顶点，形成一个较小的内部三角形。外边缘将与该内部三角形连接起来，形成三角形条。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/factors-3.png)

当细分因子为偶数时，将有一个中心顶点。当它们为奇数时，将会有一个中心三角形。如果我们使用更大的细分因子，最终会得到多个嵌套的三角形。朝向中心的每一步，三角形被细分的程度减少两个，直到最终只剩下一个或零个子边。

![4](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/factors-4.png)

### Different Edge and Inside Factors

三角形如何被细分是由内部细分因子控制的。边缘因子可用于覆盖它们各自的边缘被细分的程度。这仅影响原始patch的边缘，而不影响生成的内部三角形。为了清楚地看到这一点，将内部因子设置为7，同时保持边缘因子为1。

```
    f.edge[0] = 1;
    f.edge[1] = 1;
    f.edge[2] = 1;
	f.inside = 7;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/factors-7-inside-1-outside.png)


实际上，三角形使用因子7进行细分，然后丢弃外环的三角形。然后，每个边缘都使用自己的因子进行细分，之后生成一个三角形条以将边缘和内部三角形连接在一起。

边缘因子也可以大于内部因子。例如，将边缘因子设置为7，同时将内部因子保持为1。

```
    f.edge[0] = 7;
    f.edge[1] = 7;
    f.edge[2] = 7;
	f.inside = 1;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/factors-1-inside-7-outside.png)

在这种情况下，内部因子被强制行为就像它是2，否则就无法生成新的三角形。

### Variable Factors

硬编码的细分因子并不是很有用。所以让我们从一个单一的统一因子开始使其可配置。

```
float _TessellationUniform;

…

TessellationFactors MyPatchConstantFunction (
	InputPatch<TessellationControlPoint, 3> patch
) {
	TessellationFactors f;
    f.edge[0] = _TessellationUniform;
    f.edge[1] = _TessellationUniform;
    f.edge[2] = _TessellationUniform;
	f.inside = _TessellationUniform;
	return f;
}
```

将这个属性添加到我们的着色器中。将其范围设置为1-64。无论我们想要使用多高的因子，硬件都有64个子级的限制。

```
	_TessellationUniform ("Tessellation Uniform", Range(1, 64)) = 1
```

为了能够编辑这个因子，向MyLightingShaderGUI添加一个DoTessellation方法，以在其自己的部分显示它。

```
	void DoTessellation () {
		GUILayout.Label("Tessellation", EditorStyles.boldLabel);
		EditorGUI.indentLevel += 2;
		editor.ShaderProperty(
			FindProperty("_TessellationUniform"),
			MakeLabel("Uniform")
		);
		EditorGUI.indentLevel -= 2;
	}
```

在OnGUI中，在渲染模式和线框部分之间调用此方法。只有在所需的属性存在时才执行此操作。

```
	public override void OnGUI (
		MaterialEditor editor, MaterialProperty[] properties
	) {
		…
		DoRenderingMode();
		if (target.HasProperty("_TessellationUniform")) {
			DoTessellation();
		}
		if (target.HasProperty("_WireframeColor")) {
			DoWireframe();
		}
		…
	}
```

![inspector](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/subdividing-triangles/uniform-inspector.png)

### Fractional Factors

尽管我们使用float来设置细分因子，但我们始终得到每条边的等效细分的整数。这是因为我们使用整数分割模式。虽然这是一个很好的模式来了解细分的工作原理，但它阻止我们在细分级别之间平滑过渡。幸运的是，还有分数分割模式。让我们将模式更改为fractional_odd。

```
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
[UNITY_patchconstantfunc("MyPatchConstantFunction")]
TessellationControlPoint MyHullProgram …
```


当使用整数奇数因子时，fractional_odd分割模式产生与整数模式相同的结果。但在奇数因子之间过渡时，额外的边缘细分将被分离并增长，或者缩小并合并。这意味着边缘不再总是以相等长度的段进行分割。这种方法的优势在于细分级别之间的过渡现在是平滑的。

还可以使用fractional_even模式。它的工作方式相同，只是基于偶数因子。

fractional_odd模式经常被使用，因为它可以处理因子为1的情况，而fractional_even模式被强制使用最小级别为2。

## Tessellation Heuristics

确定最佳的细分因子是使用细分时必须问自己的主要问题。对于这个问题，没有单一的客观答案。总的来说，你能做的最好的事情就是提出一些作为启发式的度量标准，以产生良好的结果。在本教程中，我们将支持两种简单的方法。

### Edge Factors

尽管必须针对每条边提供细分因子，但并不一定要直接基于边来确定这些因子。例如，您可以根据顶点确定因子，然后在每个边上进行平均。也许这些因子存储在纹理中。无论如何，有一个单独的函数来确定给定边的两个控制点的因子是很方便的。创建这样一个函数，暂时只返回统一的值。

```
float TessellationEdgeFactor (
	TessellationControlPoint cp0, TessellationControlPoint cp1
) {
	return _TessellationUniform;
}
```

在MyPatchConstantFunction中使用这个函数来计算边缘因子。

```
TessellationFactors MyPatchConstantFunction (
	InputPatch<TessellationControlPoint, 3> patch
) {
	TessellationFactors f;
    f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
    f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
    f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	f.inside = _TessellationUniform;
	return f;
}
```

For the inside factor, we'll simply use the average of the edge factors.

```
	f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
```

### Edge Length

由于边缘细分因子控制我们对原始三角形的边进行多少细分，因此将这个因子基于这些边的长度是有意义的。例如，我们可以指定所需的三角形边长度。如果我们最终得到的三角形边比所需长度长，那么我们应该通过所需的长度对它们进行细分。添加一个变量用于这个目的。

```
float _TessellationUniform;
float _TessellationEdgeLength;
```

也添加一个属性。我们使用从0.1到1的范围，默认为0.5。这是以世界坐标单位表示的。

```
		_TessellationUniform ("Tessellation Uniform", Range(1, 64)) = 1
		_TessellationEdgeLength ("Tessellation Edge Length", Range(0.1, 1)) = 0.5
```

我们需要一个着色器特性来实现在统一细分和基于边缘的细分之间进行切换。在我们的三个通道中添加所需的指令，使用_TESSELLATION_EDGE关键字。

```
			#pragma shader_feature _TESSELLATION_EDGE
```

接下来，在MyLightingShaderGUI中添加一个枚举类型来表示细分模式。

```
	enum TessellationMode {
		Uniform, Edge
	}
```

然后调整DoTessellation，以便它可以在两种模式之间切换，使用一个枚举弹出窗口。它的工作方式类似于DoSmoothness控制平滑度模式。在这种情况下，统一是默认模式，不需要关键字。

```
	void DoTessellation () {
		GUILayout.Label("Tessellation", EditorStyles.boldLabel);
		EditorGUI.indentLevel += 2;

		TessellationMode mode = TessellationMode.Uniform;
		if (IsKeywordEnabled("_TESSELLATION_EDGE")) {
			mode = TessellationMode.Edge;
		}
		EditorGUI.BeginChangeCheck();
		mode = (TessellationMode)EditorGUILayout.EnumPopup(
			MakeLabel("Mode"), mode
		);
		if (EditorGUI.EndChangeCheck()) {
			RecordAction("Tessellation Mode");
			SetKeyword("_TESSELLATION_EDGE", mode == TessellationMode.Edge);
		}

		if (mode == TessellationMode.Uniform) {
			editor.ShaderProperty(
				FindProperty("_TessellationUniform"),
				MakeLabel("Uniform")
			);
		}
		else {
			editor.ShaderProperty(
				FindProperty("_TessellationEdgeLength"),
				MakeLabel("Edge Length")
			);
		}
		EditorGUI.indentLevel -= 2;
	}
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/edge-mode-inspector.png)

现在我们必须调整TessellationEdgeFactor。当定义了_TESSELLATION_UNIFORM时，确定两个点的世界位置，然后计算它们之间的距离。这是世界空间中的边长。边缘因子等于这个长度除以所需的长度。

```
float TessellationEdgeFactor (
	TessellationControlPoint cp0, TessellationControlPoint cp1
) {
	#if defined(_TESSELLATION_EDGE)
		float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
		float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
		float edgeLength = distance(p0, p1);
		return edgeLength / _TessellationEdgeLength;
	#else
		return _TessellationUniform;
	#endif
}
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/different-scales.png)

因为我们现在使用边长来确定边的细分因子，所以我们可能会得到每条边不同的因子。您可以看到这在四边形上发生，因为对角线边比其他边长。当使用四边形的非均匀缩放时，将其在一个维度上拉伸时，这也变得很明显。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/stretched-quad.png)

为了使这个工作，关键是共享边缘的patches必须最终使用相同的边缘细分因子。否则，在沿该边缘生成的顶点将不匹配，这可能会在网格中产生可见的间隙。在我们的情况下，我们对所有边缘使用相同的逻辑。唯一的区别可能是控制点参数的顺序。由于浮点数的限制，这在技术上可能会产生不同的因子，但差异是如此微小，以至于几乎是不可察觉的。

### Edge Length in Screen Space


虽然我们现在可以控制世界空间中的三角形边长，但这并不对应于它们在屏幕空间中的显示方式。细分的目的是在需要时添加更多的三角形。因此，我们不希望细分已经显得很小的三角形。所以让我们改用屏幕空间边长。

首先，更改我们的边长属性的范围。我们将使用像素而不是世界单位，所以像5-100这样的范围更有意义。

```
		_TessellationEdgeLength ("Tessellation Edge Length", Range(5, 100)) = 50
```

用它们的屏幕空间等效项替换世界空间计算。为此，必须将点转换为裁剪空间而不是世界空间。然后，使用它们的X和Y坐标在2D中确定它们的距离，除以它们的W坐标以将它们投影到屏幕上。

```
//		float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
//		float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
//		float edgeLength = distance(p0, p1);

		float4 p0 = UnityObjectToClipPos(cp0.vertex);
		float4 p1 = UnityObjectToClipPos(cp1.vertex);
		float edgeLength = distance(p0.xy / p0.w, p1.xy / p1.w);
		return edgeLength / _TessellationEdgeLength;
```

现在我们有了一个在裁剪空间中的结果，它是一个尺寸为2的均匀立方体，适合显示。为了转换为像素，我们必须按像素的显示尺寸进行缩放。实际上，由于显示很少是正方形的，为了获得最精确的结果，我们应该在确定距离之前单独缩放X和Y坐标。但让我们简单地通过屏幕高度进行缩放，看看效果如何。

```
	return edgeLength * _ScreenParams.y / _TessellationEdgeLength;
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/screen-space.png)

现在，我们的三角形边根据其呈现的大小进行细分。位置、旋转和缩放都相对于相机而言会影响这一点。因此，在事物运动时，细分的数量会发生变化。

### Using the View Distance


仅仅依赖边的可视长度的一个缺点是，在世界空间中很长的边在屏幕空间中可能变得非常小。这可能导致这些边根本不被细分，而其他边被细分得很多。当细分用于在近距离处添加细节或生成复杂的轮廓时，这是不可取的。

一个不同的方法是回到使用世界空间边长，但根据视距调整因子。距离越远的物体，它在视觉上应该显得越小，因此需要的细分就越少。因此，通过边和相机之间的距离除以边长。我们可以使用边的中点来确定这个距离。

```
//		float4 p0 = UnityObjectToClipPos(cp0.vertex);
//		float4 p1 = UnityObjectToClipPos(cp1.vertex);
//		float edgeLength = distance(p0.xy / p0.w, p1.xy / p1.w);
//		return edgeLength * _ScreenParams.y / _TessellationEdgeLength;

		float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
		float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
		float edgeLength = distance(p0, p1);

		float3 edgeCenter = (p0 + p1) * 0.5;
		float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

		return edgeLength / (_TessellationEdgeLength * viewDistance);
```

我们仍然可以使细分依赖于显示大小，只需将屏幕高度因子化，并保持我们的5-100滑块范围。请注意，这些值不再直接对应于显示像素。当您更改相机的视场时，这一点非常明显，它根本不影响细分。因此，这种简单的方法对于使用可变视场的游戏，例如缩放内外，效果不佳。

```
		return edgeLength * _ScreenParams.y /
			(_TessellationEdgeLength * viewDistance);
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/distance-based.png)

### Using the Correct Inside Factor

虽然细分在这一点上似乎工作得很好，但内部细分因子存在一些奇怪的问题。至少，在使用OpenGL Core时是这样。在使用均匀的四边形时不太明显，但在使用变形的立方体时就会变得明显。

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/cube-incorrect.png)


在立方体的情况下，构成一个面的两个三角形的内部细分因子各不相同。一个四边形和一个立方体面之间唯一的区别是定义三角形顶点的顺序。Unity的默认立方体不使用对称的三角形布局，而四边形使用。这表明边缘因子的顺序显然影响内部细分因子。然而，我们只是取边缘因子的平均值，因此它们的顺序不应该有关系。还必须出现其他问题。

让我们做一些看似荒谬的事情，并在计算内部因子时明确调用TessellationEdgeFactors函数。从逻辑上讲，这不应该有任何影响，因为我们最终只是进行了相同的计算两次。着色器编译器肯定会优化这个。

```
//    f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
	f.inside =
		(TessellationEdgeFactor(patch[1], patch[2]) +
		TessellationEdgeFactor(patch[2], patch[0]) +
		TessellationEdgeFactor(patch[0], patch[1])) * (1 / 3.0);
```

![img](https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/tessellation-heuristics/cube-correct.png)


显然，这确实有所不同，因为现在两个面三角形最终使用了几乎相同的内部因子。这里发生了什么？

补丁常量函数与其余的hull shader并行调用。但事实上，情况可能比这更复杂。着色器编译器能够将边缘因子的计算并行化。MyPatchConstantFunction中的代码被撕裂并部分复制，用一个分叉的进程替换，以并行计算三个边缘因子。一旦所有三个进程完成，它们的结果就会合并，并用于计算内部因子。

编译器是否决定分叉进程不应该影响我们的着色器的结果，只会影响其性能。不幸的是，OpenGL Core生成的代码中存在错误。在计算内部因子时，不是使用三个边缘因子，而是只使用第三个边缘因子。数据是存在的，只是访问了三次索引2，而不是索引0、1和2。因此，我们总是得到一个等于第三个边缘因子的内部因子。

在补丁常量函数的情况下，着色器编译器优先考虑并行化。它尽早拆分进程，之后不能再优化掉TessellationEdgeFactor的重复调用。我们最终得到三个进程，每个进程都计算两个点的世界位置、距离和最终因子。然后还有计算内部因子的过程，它现在还必须计算三个点的世界位置，以及涉及的所有距离和因子。由于我们现在已经为内部因子做了所有这些工作，因此也没有意义为边缘因子的一部分分别做同样的工作。

事实证明，如果我们首先计算点的世界位置，然后分别为边缘和内部因子调用TessellationEdgeFactor，那么着色器编译器将决定不为每个边缘因子分叉单独的进程。我们最终得到一个计算所有的单一进程。在这种情况下，着色器编译器确实优化掉了TessellationEdgeFactor的重复调用。

```
float TessellationEdgeFactor (float3 p0, float3 p1) {
	#if defined(_TESSELLATION_EDGE)
//		float3 p0 = mul(unity_ObjectToWorld, cp0.vertex).xyz;
//		float3 p1 = mul(unity_ObjectToWorld, cp1.vertex).xyz;
		…
	#else
		return _TessellationUniform;
	#endif
}

TessellationFactors MyPatchConstantFunction (
	InputPatch<TessellationControlPoint, 3> patch
) {
	float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
	float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
	float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;
	TessellationFactors f;
    f.edge[0] = TessellationEdgeFactor(p1, p2);
    f.edge[1] = TessellationEdgeFactor(p2, p0);
    f.edge[2] = TessellationEdgeFactor(p0, p1);
	f.inside =
		(TessellationEdgeFactor(p1, p2) +
		TessellationEdgeFactor(p2, p0) +
		TessellationEdgeFactor(p0, p1)) * (1 / 3.0);
	return f;
}
```

到目前为止，我们可以细分三角形，但我们还没有利用这个能力。表面位移演示了细分如何用于形变表面。