﻿using System.Collections.Concurrent;
using CMS.DataEngine;
using Kentico.Xperience.UMT.ProviderProxy;

namespace Kentico.Xperience.UMT;

public class FakeProviderProxyFactory : IProviderProxyFactory
{
    private readonly FakeProviderProxy fakeProxy;

    public FakeProviderProxyFactory(ProviderProxyContext proxyContext) => fakeProxy = new FakeProviderProxy(proxyContext);

    public IProviderProxy CreateProviderProxy<TInfo>(ProviderProxyContext context) where TInfo : BaseInfo => fakeProxy;

    public IProviderProxy CreateProviderProxy(Type? infoType, ProviderProxyContext context) => fakeProxy;
}

public class FakeProviderProxy: IProviderProxy
{
    private readonly List<BaseInfo> fakedObjects = new();
    private readonly ConcurrentDictionary<Type, int> providedIds = new();
    
    public FakeProviderProxy(ProviderProxyContext context) => Context = context;

    public BaseInfo? GetBaseInfoByGuid(Guid guid) => fakedObjects.FirstOrDefault(x => guid.Equals(x[x.TypeInfo.GUIDColumn]));

    public BaseInfo? GetBaseInfoBy(Guid guid, string searchedField) => fakedObjects.FirstOrDefault(x => guid.Equals(x[searchedField ?? x.TypeInfo.GUIDColumn]));

    public BaseInfo Save(BaseInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        
        fakedObjects.Add(info);
        info[info.TypeInfo.IDColumn] = providedIds.AddOrUpdate(info.GetType(), type => 1, (type, i) => i + 1);
        return info;
    }

    public ProviderProxyContext Context { get; }
}
