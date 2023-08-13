﻿using CMS.DataEngine;
using CMS.DocumentEngine;
using Kentico.Xperience.UMT.Model;
using Kentico.Xperience.UMT.ProviderProxy;
using Kentico.Xperience.UMT.Services.Model;
using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.UMT.InfoAdapter;

internal class TreeNodeAdapter: GenericInfoAdapter<TreeNode>
{
    private readonly IProviderProxyFactory providerProxyFactory;

    internal TreeNodeAdapter(ILogger<TreeNodeAdapter> logger, UmtModelService modelService, IProviderProxy providerProxy, IProviderProxyFactory providerProxyFactory) : base(logger, modelService, providerProxy, providerProxyFactory) 
        => this.providerProxyFactory = providerProxyFactory;

    protected override TreeNode ObjectFactory(UmtModelInfo umtModelInfo, IUmtModel umtModel)
    {
        if (umtModel is TreeNodeModel tnm && tnm.NodeClassGuid is {} dataClassGuid)
        {
            var dataClassProxy = providerProxyFactory.CreateProviderProxy<DataClassInfo>(ProviderProxy.Context);
            var dataClass = dataClassProxy.GetBaseInfoByGuid(dataClassGuid);
            if (dataClass is DataClassInfo dci)
            {
                return TreeNode.New(dci.ClassName);
            }
        }
        
        return TreeNode.New();
    }
}