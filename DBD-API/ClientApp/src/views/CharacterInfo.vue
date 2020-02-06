<template>
  <div class="base-container character-info">
    <div class="baseInfo">
      <div class="content-container">
        <img :src="icon" />
        <div class="desc">
          <h1>{{name}}</h1>
          <h3><b>Role:</b> {{role}}</h3>
          <h3><b>Difficulty:</b> {{difficulty}}</h3>
          <h3><b>Gender:</b> {{gender}}</h3>
          <h3><b>Height:</b> {{height}}</h3>
          <h3 v-if="hasPowerInfo"><b>Power:</b> {{powerInfo.name}}</h3>
        </div>
      </div>
    </div>
    <div class="specificInfo">
      <div class="container">
        <a-tabs>
          <a-tab-pane tab="Overview" key="1" class="overview">
            <h3 v-if="hasPowerInfo">Power</h3>
            <span v-if="hasPowerInfo" v-html="powerInfo.description" ></span>
            <h3>Backstory</h3>
            <span v-html="backstory"></span>
            <h3>Biography</h3>
            <span v-html="biography"></span>
          </a-tab-pane>
          <a-tab-pane tab="Perks" key="2">
            <PerkList :perks="perks" :branch="branch" :showUnlocks="true" />
          </a-tab-pane>
          <a-tab-pane tab="Items" key="4" v-if="role !== 'Killer'">
            <AddonsList :addons="items" :branch="branch" />
          </a-tab-pane>
          <a-tab-pane tab="Addons" key="5">
            <AddonsList :addons="addons" :branch="branch" />
          </a-tab-pane>
          <a-tab-pane tab="Tunables" key="6" :disabled="role !== 'Killer'">
            <TunablesTable :tunables="killerTunables" :branch="branch" />
          </a-tab-pane>
        </a-tabs>
      </div>
    </div>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";
  import PerkList from "../components/PerkList";
  import AddonsList from "../components/AddonsList";
  import TunablesTable from "../components/TunablesTable";

  export default {
    name: "CharacterInfo",
    components: {
      PerkList,
      AddonsList,
      TunablesTable
    },

    data() {
      return {
        branch: "live",
        characterIndex: this.$route.params.id,
        hasPowerInfo: false,

        name: "",
        icon: "",
        role: "",
        gender: "",
        height: "",
        difficulty: "",
        biography: "",
        backstory: "",

        powerInfo: {
          name: "",
          description: "",
        },

        killerTunables: {
          baseTunables:  {},
          tunableValues: {},
          tunables:      {},
        },

        perks: [],
        addons: [],
        items: []
      }
    },

    methods: {
      fetchCharacterInfo(){
        ApiService.getCharacterByIndex(this.characterIndex, this.branch)
          .then(data => {
            this.name = data.displayName;
            this.icon = ApiService.getIconUrl(this.branch, data.iconPath || "");
            this.role = ApiService.convertPlayerRole(data.role);
            this.gender = ApiService.convertGender(data.gender);
            this.height = ApiService.convertKillerHeight(data.killerHeight);
            this.difficulty = ApiService.convertCharacterDifficulty(data.difficulty);
            this.backstory = data.backStory;
            this.biography = data.biography;

            let addonsPromise = this.role === "Survivor" ?
              ApiService.getSurvivorAddons(this.branch, data.characterIndex) :
              ApiService.getKillerAddons(this.branch, data.defaultItem);

            addonsPromise
              .then(data => {
                this.addons = data;
              })
              .catch(ex => console.warn("WARNING failed to fetch perks for reason:", ex));


            if(this.role === "Killer") {
              ApiService.getItem(data.defaultItem, this.branch)
                .then(data => {
                  this.powerInfo.name = data.displayName;
                  this.powerInfo.description = data.description;
                  this.hasPowerInfo = true;
                })
                .catch(ex => console.warn("WARNING failed to get killer power info:", ex));

              ApiService.getKillerTunables(this.branch, data.idName)
                .then(data => {
                  this.killerTunables = data;
                  console.log("got tunables", data);
                })
                .catch(ex => console.warn("WARNING failed to get killer tunables:", ex))
            } else {
              ApiService.getSurvivorItems(this.branch)
                .then(data => {
                  this.items = data;
                })
                .catch(ex => console.warn("WARNING failed to get survivor items:", ex));
            }

            ApiService.getCharacterPerks(data.characterIndex, this.branch)
              .then(data => {
                this.perks = data;
              })
              .catch(ex => console.warn("WARNING failed to fetch perks for reason:", ex));
          })
          .catch(ex => {
            console.warn("WARNING failed to fetch character info reason:", ex);
            this.$router.push("/characters");
          });
      }
    },

    mounted(){
      if(this.characterIndex < 0)
        this.$router.push("/characters");
      else
        this.fetchCharacterInfo();
    }
  }
</script>

<style scoped lang="scss">
  div.base-container {
    display: flex;
    flex-direction: column;
    width: 100%;

    & > div.baseInfo {
      width: 100%;
      background: rgba(0,0,0,0.4);

      & div.content-container {
        display: flex;
        flex-direction: row;
        padding-bottom: 40px;

        & img {
          flex: 0 0 auto;
          width: 200px;
          border: 1px solid rgba(0,0,0,0.4);
          background: rgba(0,0,0,0.4);
          border-radius: 5px;
        }

        & div.desc {
          flex: 1 1 100%;
          margin-left: 20px;
          display: flex;
          flex-direction: column;

          h3 > b {
            display: inline-block;
            width: 90px;
          }
        }
      }
    }

    & > div.specificInfo {
      & > div.container {
       padding-bottom: 40px;

       div.overview {
         h3 {
           color: white;
           font-weight: 700;

           &:not(:first-of-type) {
             margin-top: 40px;
           }
         }
         span {
           display: block;
           margin-left: 20px;
         }
       }

       & > .ant-tabs {
         color: white;
       }
      }
    }
  }
</style>