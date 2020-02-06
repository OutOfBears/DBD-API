<template>
  <div class="content-container">
    <a-alert
            v-if="error !== ''"
            message="Error loading offerings..."
            :description="error"
            type="error"
    />
    <div class="header" v-if="survivorOfferings.length > 0 || loading">
      <div class="bar">
        <h1 v-if="!loading">Survivor Offerings</h1>
        <h1 v-if="loading">Offerings</h1>
        <a-input placeholder="Enter search term" v-model="offeringSearch" />
      </div>
      <OfferingList :loading="loading" :branch="branch" :offerings="survivorOfferings" />
    </div>
    <div class="header" v-if="killerOfferings.length > 0 && !loading">
      <div class="bar">
        <h1>Killer Offerings</h1>
        <a-input placeholder="Enter search term" v-model="offeringSearch" v-if="survivorOfferings.length < 1" />
      </div>
      <OfferingList :loading="loading" :branch="branch" :offerings="killerOfferings" />
    </div>
    <div class="header" v-if="killerOfferings.length < 1 && survivorOfferings.length < 1 && !loading">
      <div class="bar">
        <h1>Offerings</h1>
        <a-input placeholder="Enter search term" v-model="offeringSearch" />
      </div>
      <span class="results" v-if="error === ''">Search yielded no results..</span>
      <span class="results" v-if="error !== ''">Failed to get offerings</span>
    </div>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";
  import OfferingList from "../components/OfferingList";

  const filterSearch = function(searchTerm, arr) {
    const escapeRegExp = function(string) {
      return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    };

    return arr.filter((x) => {
      let search = new RegExp(escapeRegExp(searchTerm), "mi");
      let containsSearchTerm = search.test(x.displayName) ||
         search.test(x.description);

      return searchTerm.trim() === '' ? true : containsSearchTerm;
    })
  };

  export default {
    name: "Offerings",
    components: {
      OfferingList
    },

    data() {
      return {
        offeringsList: [],
        loading: false,
        error: "",
        branch: "live",
        offeringSearch: "",
      }
    },

    mounted(){
      this.fetchOfferings();
    },

    computed: {
      survivorOfferings: function(){
        return filterSearch(this.offeringSearch, this.offeringsList.filter(x => x.role === "EPlayerRole::VE_Camper"));
      },
      killerOfferings: function() {
        return filterSearch(this.offeringSearch, this.offeringsList.filter(x => x.role === "EPlayerRole::VE_Slasher"));
      }
    },

    methods: {
      fetchOfferings() {
        this.loading = true;

        ApiService.getOfferings(this.branch)
          .then(data => {
            this.offeringsList = Object.values(data);
            this.loading = false;

            console.log("offerings",  this.offeringsList);
          })
          .catch(ex => {
            console.warn("WARNING failed to fetch offerings:", ex);
            this.error = ex.toString();
            this.loading = false;
          })
      }
    },

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