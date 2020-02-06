<template>
  <div class="tunables-table">
    <div class="tunable-container" v-if="tunableValues.length > 0">
      <h2>Tunable Values</h2>
      <a-table :columns="columns" :dataSource="tunableValues"></a-table>
    </div>
    <div class="tunable-container" v-if="formattedTunables.length > 0">
      <h2>Killer Tunables</h2>
      <a-table :columns="columns" :dataSource="formattedTunables"></a-table>
    </div>
    <div class="tunable-container" v-if="baseTunables.length > 0">
      <h2>Base Killer Tunables</h2>
      <p>(These apply to every killer)</p>
      <a-table :columns="columns" :dataSource="baseTunables"></a-table>
    </div>
  </div>
</template>

<script>
  const mapObjectToArray = (obj) => {
    let arr = [];
    for(let i in obj)
      arr.push({ id: i, ...obj[i] });

    return arr;
  };

  export default {
    name: "TunablesTable",
    props: {
      tunables: Array,
      branch: String
    },
    computed: {
      tunableValues: function() { return mapObjectToArray(this.tunables.tunableValues || {}) },
      formattedTunables: function() { return mapObjectToArray(this.tunables.tunables || {}) },
      baseTunables: function() { return mapObjectToArray(this.tunables.baseTunables || {}) },
    },
    
    data: function () {
      return {
        columns: [
          {
            title: 'Name',
            dataIndex: 'id',
          },
          {
            title: 'Description',
            dataIndex: 'description',
          },
          {
            title: 'Tags',
            dataIndex: 'descriptorTags',
          },
          {
            title: 'Value',
            dataIndex: 'value',
          },
        ]
      }
    }

  }
</script>

<style scoped lang="scss">
  div.tunables-table {
    & h2 {
      color: white;
    }
    & p {
      margin-top: -0.5em;
      font-size: .8em;
      color: rgba(255,255,255,0.6)
    }
  }
</style>