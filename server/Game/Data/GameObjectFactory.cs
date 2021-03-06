using Game.Data.Stats;
using Game.Data.Troop;
using Game.Setup;
using Game.Setup.DependencyInjection;
using Persistance;

namespace Game.Data
{
    class GameObjectFactory : IGameObjectFactory
    {
        private readonly IKernel kernel;

        private readonly IStructureCsvFactory structureCsvFactory;

        private readonly ITechnologyManagerFactory technologyManagerFactory;

        private readonly IDbManager dbManager;

        public GameObjectFactory(IKernel kernel, IStructureCsvFactory structureCsvFactory, ITechnologyManagerFactory technologyManagerFactory)
        {
            this.kernel = kernel;
            this.structureCsvFactory = structureCsvFactory;
            this.technologyManagerFactory = technologyManagerFactory;
            
            dbManager = kernel.Get<IDbManager>();
        }

        public IStructure CreateStructure(uint cityId, uint structureId, ushort type, byte level, uint x, uint y, string theme)
        {
            var baseStats = structureCsvFactory.GetBaseStats(type, level);
            var technologyManager = technologyManagerFactory.CreateTechnologyManager(EffectLocation.Object, cityId, structureId);
            var structureProperties = new StructureProperties(cityId, structureId);

            var structure = new Structure(structureId,
                                          new StructureStats(baseStats),
                                          x,
                                          y,
                                          theme,
                                          technologyManager,
                                          structureProperties,
                                          dbManager);
            
            return structure;
        }

        public ITroopObject CreateTroopObject(uint id, ITroopStub stub, uint x, uint y, string theme)
        {
            return new TroopObject(id, stub, x, y, theme, kernel.Get<IDbManager>());            
        }
    }
}