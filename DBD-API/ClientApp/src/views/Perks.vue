<template>
  <div class="content-container">
    <a-alert
        v-if="error !== ''"
        message="Error loading perks..."
        :description="error"
        type="error"
    />

    <div class="header" v-if="survivorPerksList.length > 0 || loading">
      <div class="bar">
        <h1 v-if="!loading">Survivor Perks</h1>
        <h1 v-if="loading">Perks</h1>
        <a-input placeholder="Enter search term" v-model="perkSearch" />
      </div>
      <PerkList :perks="survivorPerksList" :loading="loading" :branch="this.branch" />
    </div>
    <div class="header" v-if="killerPerksList.length > 0 && !loading">
      <div class="bar">
        <h1>Killer Perks</h1>
        <a-input placeholder="Enter search term" v-model="perkSearch" v-if="survivorPerksList.length < 1" />
      </div>
      <PerkList :perks="killerPerksList" :loading="loading" :branch="this.branch" />
    </div>
    <div class="header" v-if="killerPerksList.length < 1 && survivorPerksList.length < 1 && !loading">
      <div class="bar">
        <h1>Perks</h1>
        <a-input placeholder="Enter search term" v-model="perkSearch" />
      </div>
      <span class="results" v-if="error === ''">Search yielded no results..</span>
      <span class="results" v-if="error !== ''">Failed to get offerings</span>
    </div>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";
  import PerkList from "../components/PerkList";

  const filterSearch = function(searchTerm, arr) {
    const escapeRegExp = function(string) {
      return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    };

    return arr.filter((x) => {
      let search = new RegExp(escapeRegExp(searchTerm), "mi");
      let containsSearchTerm = search.test(x.displayName) ||
        search.test(x.perkDefaultDescription) ||
        search.test(x.perkLevel1Description) ||
        search.test(x.perkLevel2Description) ||
        search.test(x.perkLevel3Description);

      return searchTerm.trim() === '' ? true : containsSearchTerm;
    })
  };

  export default {
    name: "Perks",
    components: {
      PerkList
    },

    data() {
      return {
        perkSearch: "",
        branch: "live",
        error: "",
        perks: [],
        loading: false,
      }
    },

    computed: {
      survivorPerksList: function(){
        return filterSearch(this.perkSearch, this.perks.filter((x) => x.id !== "Last_Standing" && x.type === "EInventoryItemType::CamperPerk"));
      },
      killerPerksList: function(){
        return filterSearch(this.perkSearch, this.perks.filter((x) => x.type === "EInventoryItemType::SlasherPerk"));
      }
    },

    methods: {
      fetchPerks(){
        this.loading = true;
        this.error = "";

        ApiService.getPerks(this.branch)
          .then(data => {
            this.perks = Object.values(data);
            this.loading = false;
          })
          .catch(ex => {
            this.error = ex.toString();
            this.loading = false;
          })
      }
    },

    mounted(){
      this.fetchPerks();
    }
  }
</script>

<style scoped lang="scss">
  div.content-container {
    div.header {
      & > div.bar {
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: space-between;

        & > input {
          width: 45%;
          margin-bottom: 0.5em;
          background: rgba(0, 0, 0, .2);
          border-color: rgba(0, 0, 0, .2);
          color: #fff;
        }
      }
      & > span.results {
        padding-top: 60px;
        text-align: center;
        width: 100%;
        display: block;
        font-size: 1.2em;
        color: rgba(255,255,255,0.6);
      }
    }
  }
</style>