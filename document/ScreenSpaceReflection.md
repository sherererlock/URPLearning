# ScreenSpaceReflection

## URP实现

### c#

#### 疑问

1. Blit是否不会检查ConfigureTarget的配置，而是将内容直接渲染在Blit设置的RT上

2. Blit中的loadAction和StoreAction指的是作用于哪个buffer的Action？默认值是什么？

3. Execute中获取到renderingData中的cmd后还需要显示调用ExecuteCommandBuffer？

   