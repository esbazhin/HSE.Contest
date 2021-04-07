Vue.component('all_classes', {
    props: ['all_classes'],
    data() {
        return {
            activeKey: this.all_classes[0].key,
            newTabIndex: 2
        };
    },

    methods: {
        onEdit(targetKey, action) {
            this[action](targetKey);
        },
        add() {
            const all_classes = this.all_classes;
            const activeKey = this.newTabIndex++;
            all_classes.push(new TypeInfo(activeKey));
            this.all_classes = all_classes;
            this.activeKey = activeKey;
        },
        remove(targetKey) {
            let activeKey = this.activeKey;
            let lastIndex;
            this.all_classes.forEach((pane, i) => {
                if (pane.key == targetKey) {
                    lastIndex = i - 1;
                }
            });
            this.all_classes.splice(lastIndex + 1, 1);
            const panes = this.all_classes;
            if (panes.length > 0) {
                if (activeKey == targetKey) {
                    if (lastIndex > 0) {
                        activeKey = panes[lastIndex].key;
                    } else {
                        activeKey = panes[0].key;
                    }
                }
            } else {
                activeKey = 0;
            }
            this.all_classes = panes;
            this.activeKey = activeKey;
        },
    },
    template: `
  <div>
  <a-tabs v-model="activeKey" type="editable-card" @edit="onEdit">
    <a-tab-pane v-for="item in all_classes" :tab="item.name" :key="item.key">
		<a-tabs defaultActiveKey="1" tabPosition="left">
			<a-tab-pane tab="Общая информация" key="1">
				<common_info :cur_class="item"></common_info>
			</a-tab-pane>
			<a-tab-pane tab="Поля" key="2">
				<all_fields :fields="item.fields" :isEvent="false"></></all_fields>
			</a-tab-pane>
			<a-tab-pane tab="Методы" key="3">
				<all_methods :methods="item.methods"></all_methods>
			</a-tab-pane>
			<a-tab-pane tab="Конструкторы" key="4">
				<all_constrs :constrs="item.constructors"></all_constrs>
			</a-tab-pane>
			<a-tab-pane tab="Свойства" key="5">
				<all_properties :properties="item.properties"></all_properties>
			</a-tab-pane>
			<a-tab-pane tab="События" key="6">
				<all_fields :fields="item.events" :isEvent="true"></all_fields>
			</a-tab-pane>
		</a-tabs>
    </a-tab-pane>
  </a-tabs>
  </div>`,
});

Vue.component('all_fields', {
    props: ['fields', 'isEvent'],
    data() {
        return {
            columns: [
                {
                    title: 'Область видимости',
                    dataIndex: 'visibility',
                    scopedSlots: { customRender: 'visibility' },
                },
                {
                    title: 'Модификатор доступа',
                    dataIndex: 'modifier',
                    scopedSlots: { customRender: 'modifier' },
                },
                {
                    title: 'Тип',
                    dataIndex: 'type',
                    scopedSlots: { customRender: 'type' },
                },
                {
                    title: 'Имя',
                    dataIndex: 'name',
                    scopedSlots: { customRender: 'name' },
                },
                {
                    dataIndex: 'delete',
                    scopedSlots: {
                        customRender: 'delete',
                    },
                    slots: {
                        title: 'addTitle'
                    }
                }
            ],
        };
    },
    methods: {
        add: function () {
            let last = 0;
            if (this.fields.length > 0) {
                last = this.fields[this.fields.length - 1].key;
            }
            this.fields.push(new FieldInfo(last + 1, this.isEvent));
        }
    },
    computed: {
        getDescription: function () {
            return this.isEvent ? 'Нет событий' : 'Нет полей';
        },
        getBtn: function () {
            return this.isEvent ? 'Добавить событие' : 'Добавить поле';
        }
    },
    template: `
<div class="all_fields"> 
  <div v-if="fields.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
<span slot="description">{{getDescription}}</span>
    <a-button @click="add" type="primary">{{getBtn}}</a-button>
  </a-empty>
  </div>
  <div v-else>
    <a-table bordered :dataSource="fields" :columns="columns">
	<template slot="addTitle">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>
	</a-button>
	</template>
      <template slot="visibility" slot-scope="text, record">
        <a-input allowClear v-model="record.visibility" />
      </template>
	  
	  <template slot="modifier" slot-scope="text, record">
        <a-input allowClear v-model="record.modifier" />
      </template>
	  
	  <template slot="type" slot-scope="text, record">
        <a-input allowClear v-model="record.type" />
      </template>
	  
	  <template slot="name" slot-scope="text, record">
        <a-input allowClear v-model="record.name" />
      </template>
	  
	  <template slot="delete" slot-scope="text, record, index">
        <a-popconfirm
          v-if="fields.length"
          title="Sure to delete?"
          @confirm="fields.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
      </template>
      
    </a-table>
</div>
  </div>`
});

Vue.component('all_methods', {
    props: ['methods'],
    data() {
        return {
            columns: [
                {
                    title: 'Область видимости',
                    dataIndex: 'visibility',
                    scopedSlots: { customRender: 'visibility' },
                },
                {
                    title: 'Модификатор доступа',
                    dataIndex: 'modifier',
                    scopedSlots: { customRender: 'modifier' },
                },
                {
                    title: 'Возвращаемый тип',
                    dataIndex: 'returnType',
                    scopedSlots: { customRender: 'type' },
                },
                {
                    title: 'Имя',
                    dataIndex: 'name',
                    scopedSlots: { customRender: 'name' },
                },
                {
                    title: 'Кол-во параметров',
                    dataIndex: 'parameters.length',

                },
                {
                    dataIndex: 'delete',
                    scopedSlots: {
                        customRender: 'delete',
                    },
                    slots: {
                        title: 'addTitle'
                    }
                }
            ]
        };
    },
    methods: {
        add: function () {
            let last = 0;
            if (this.methods.length > 0) {
                last = this.methods[this.methods.length - 1].key;
            }
            this.methods.push(new MethodInfo(last + 1, false));
        }
    },
    template: `
<div class="all_methods"> 
  <div v-if="methods.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет методов</span>
    <a-button @click="add" type="primary">Добавить метод</a-button>
  </a-empty>
  </div>
  <div v-else>
    <a-table bordered :dataSource="methods" :columns="columns">
	<template slot="addTitle">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>
	</a-button>
	
	</template>
      <template slot="visibility" slot-scope="text, record">
        <a-input allowClear v-model="record.visibility" />
      </template>
	  
	  <template slot="modifier" slot-scope="text, record">
        <a-input allowClear v-model="record.modifier" />
      </template>
	  
	  <template slot="type" slot-scope="text, record">
        <a-input allowClear v-model="record.returnType" />
      </template>
	  
	  <template slot="name" slot-scope="text, record">
        <a-input allowClear v-model="record.name" />
      </template>
	  
	  <template slot="delete" slot-scope="text, record, index">
        <a-popconfirm
          v-if="methods.length"
          title="Sure to delete?"
          @confirm="methods.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
      </template>
	  
	
	<template slot="expandedRowRender" slot-scope="text, record, index">
        <all_params :params="text.parameters" :mName="'метода ' + text.name"></all_params>
      </template>
	
	<template slot="expandIcon" slot-scope="props">
        <a-icon type="minus"></a-icon>
      </template>
	
      
    </a-table>
</div>
  </div>`
});

Vue.component('all_params', {
    props: ['params', 'mName'],
    data() {
        return {
            columns: [
                {
                    title: 'Тип',
                    dataIndex: 'type',
                    scopedSlots: { customRender: 'type' },
                },
                {
                    title: 'Имя',
                    dataIndex: 'name',
                    scopedSlots: { customRender: 'name' },
                },
                {
                    title: 'Позиция',
                    dataIndex: 'position',
                    scopedSlots: { customRender: 'position' },
                },
                {
                    title: 'Значение по умолчанию',
                    dataIndex: 'defaultValue',
                    scopedSlots: { customRender: 'defaultValue' },
                },
                {
                    title: 'Является Out',
                    dataIndex: 'isOut',
                    scopedSlots: { customRender: 'isOut' },
                },
                {
                    dataIndex: 'delete',
                    scopedSlots: {
                        customRender: 'delete',
                    },
                    slots: {
                        title: 'addTitle'
                    }
                }

            ]
        };
    },
    methods: {
        add: function () {
            let last = 0;
            let pos = 0;
            if (this.params.length > 0) {
                pos = this.params.length;
                last = this.params[pos - 1].key;
            }
            this.params.push(new ParameterInfo(pos, last + 1));
        }
    },
    template: `
  <div class="all_params"> 
  <div v-if="params.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет параметров</span>
    <a-button @click="add" type="primary">Добавить параметр</a-button>
  </a-empty>
  </div>
  <div v-else>
    <a-table  bordered :pagination="{pageSize: 2}" size="small" :dataSource="params" :columns="columns">
	<template slot="title" slot-scope="currentPageData">
      Параметры {{mName}} 
    </template>
	<template slot="addTitle">
		<a-button @click="add" type="primary">
		<a-icon type="plus"></a-icon>
		</a-button>
	</template>
	
	<template slot="type" slot-scope="text, record">
        <a-input allowClear v-model="record.type" />
    </template>
	  
	  <template slot="name" slot-scope="text, record">
        <a-input allowClear v-model="record.name" />
      </template>
	  
	  <template slot="position" slot-scope="text, record">
        <a-input-number :min="0"  v-model="record.position" />
      </template>
	  
	  <template slot="defaultValue" slot-scope="text, record">
        <a-input allowClear v-model="record.defaultValue" />
      </template>
	  
	  <template slot="isOut" slot-scope="text, record">
        <a-switch v-model="record.isOut" />
      </template>
	  
	  <template slot="delete" slot-scope="text, record, index">
        <a-popconfirm
          v-if="params.length"
          title="Sure to delete?"
          @confirm="params.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
      </template>
      
    </a-table>
</div>
  </div>`
});

Vue.component('all_constrs', {
    props: ['constrs'],
    data() {
        return {
            columns: [
                {
                    title: 'Область видимости',
                    dataIndex: 'visibility',
                    scopedSlots: { customRender: 'visibility' },
                },
                {
                    title: 'Модификатор доступа',
                    dataIndex: 'modifier',
                    scopedSlots: { customRender: 'modifier' },
                },
                {
                    title: 'Кол-во параметров',
                    dataIndex: 'parameters.length',

                },
                {
                    dataIndex: 'delete',
                    scopedSlots: {
                        customRender: 'delete',
                    },
                    slots: {
                        title: 'addTitle'
                    }
                }
            ]
        };
    },
    methods: {
        add: function () {
            let last = 0;
            if (this.constrs.length > 0) {
                last = this.constrs[this.constrs.length - 1].key;
            }
            this.constrs.push(new ConstructorInfo(last));
        }
    },
    template: `
<div class="all_constrs"> 
  <div v-if="constrs.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет конструкторов</span>
    <a-button @click="add" type="primary">Добавить конструктор</a-button>
  </a-empty>
  </div>
  <div v-else>
    <a-table bordered :dataSource="constrs" :columns="columns">
	<template slot="addTitle">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>
	</a-button>
	
	</template>
      <template slot="visibility" slot-scope="text, record">
        <a-input allowClear v-model="record.visibility" />
      </template>
	  
	  <template slot="modifier" slot-scope="text, record">
        <a-input allowClear v-model="record.modifier" />
      </template>
	  
	  <template slot="delete" slot-scope="text, record, index">
        <a-popconfirm
          v-if="constrs.length"
          title="Sure to delete?"
          @confirm="constrs.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
      </template>
	  
	
	<template slot="expandedRowRender" slot-scope="text, record, index">
        <all_params :params="text.parameters" :mName="'конструктора ' + record"></all_params>
      </template>
	
	<template slot="expandIcon" slot-scope="props">
        <a-icon type="minus"></a-icon>
      </template>
	
      
    </a-table>
</div>
  </div>`
});

Vue.component('common_info', {
    props: ['cur_class'],
    data() {
        return {
            formItemLayout: {
                labelCol: {
                    xs: { span: 7 },
                    sm: { span: 7 },
                },
                wrapperCol: {
                    xs: { span: 9 },
                    sm: { span: 9 },
                },
            },
            intItemLayout: {
                labelCol: {
                    xs: { span: 4 },
                    sm: { span: 4 },
                },
                wrapperCol: {
                    xs: { span: 12 },
                    sm: { span: 12 },
                },
            },
            formItemLayoutWithOutLabel: {
                wrapperCol: {
                    xs: { span: 12, offset: 4 },
                    sm: { span: 12, offset: 4 },
                },
            },
            intBtn: {
                wrapperCol: {
                    xs: { span: 16, offset: 4 },
                    sm: { span: 16, offset: 4 },
                },
            }
        }
    },
    methods: {
        add: function () {
            this.cur_class.implementedInterfaces.push('');
        }
    },
    template: `
<div class="common_info"> 
<a-form>
<a-row>
<a-col :span="12">
	<a-form-item
        label="Имя класса: "
		v-bind="formItemLayout"
		required
        >
        <a-input allowClear type="text" v-model="cur_class.name" /> 
      </a-form-item>
	  <a-form-item
        label="Базовый класс: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="cur_class.base" /> 
		</a-form-item>
		
	<a-form-item
        label="Область видимости: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="cur_class.visibility" />
      </a-form-item>
	  
	  <a-form-item
        label="Модификатор доступа: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="cur_class.modifier" />
      </a-form-item>
	 </a-col>
	 <a-col :span="12">
	  
	  <a-form-item
      v-for="(k, index) in cur_class.implementedInterfaces"
	  :key="index"
      v-bind="index === 0 ? intItemLayout : formItemLayoutWithOutLabel"
      :label="index === 0 ? 'Интерфейсы:' : ''"
    >

	<span>
	<a-input allowClear type="text" v-model="cur_class.implementedInterfaces[index]" style="width: 60%; margin-right: 8px"/>
	<a-popconfirm
          title="Sure to delete?"
          @confirm="cur_class.implementedInterfaces.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
		</span>
	</a-form-item>
	
	<a-form-item v-bind="intBtn">
      <a-button type="dashed" style="width: 60%" @click="add">
        <a-icon type="plus" /> Добавить интерфейс
      </a-button>
    </a-form-item>
	</a-col>
</a-row>
</a-form>
  </div>`
});

Vue.component('property', {
    props: ['property'],
    data() {
        return {
            formItemLayout: {
                labelCol: {
                    xs: { span: 9 },
                    sm: { span: 9 },
                },
                wrapperCol: {
                    xs: { span: 9 },
                    sm: { span: 9 },
                },
            },
            delBtn: {
                wrapperCol: {
                    xs: { span: 9, offset: 9 },
                    sm: { span: 9, offset: 9 },
                },
            }
        }
    },
    methods: {
        addG: function () {
            let get = new MethodInfo(0);
            get.returnType = this.property.type;
            get.name = 'get_' + this.property.name;
            this.property.getMethod = get;
        },

        addS: function () {
            let set = new MethodInfo(0);
            set.returnType = this.property.type;
            set.name = 'set_' + this.property.name;

            let param = new ParameterInfo(0, 0);
            param.type = this.property.type;
            param.name = "value";

            set.parameters.push(param);
            this.property.setMethod = set;
        },
        changeN: function () {
            if (this.property.getMethod) {
                this.property.getMethod.name = 'get_' + this.property.name;
            }
            if (this.property.setMethod) {
                this.property.setMethod.name = 'set_' + this.property.name;
            }
        },
        changeT: function () {
            if (this.property.getMethod) {
                this.property.getMethod.returnType = this.property.type;
            }
            if (this.property.setMethod) {
                this.property.setMethod.returnType = this.property.type;
                this.property.setMethod.parameters[0].type = this.property.type;
            }
        }
    },
    template: `
<div class="property_div"> 
<a-card>
<template slot="title">
		<slot></slot>
<label>{{property.name}}</label>
</template>
<a-form>
<a-row>
<a-col :span="8">
	<a-form-item
        label="Имя свойства: "
		v-bind="formItemLayout"
		required
        >
        <a-input allowClear type="text" @change="changeN" v-model="property.name" /> 
      </a-form-item>
	  <a-form-item
        label="Тип: "
		v-bind="formItemLayout"
		required
        >
        <a-input allowClear type="text" @change="changeT" v-model="property.type" /> 
		</a-form-item>
</a-col>
<a-col :span="8">
	  	<div v-if="!property.getMethod">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет геттера</span>
    <a-button @click="addG" type="primary">Добавить геттер</a-button>
  </a-empty>
  </div>
  <div v-else>	
  <a-form-item
        label="Область видимости: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="property.getMethod.visibility" /> 
      </a-form-item>
	  <a-form-item
        label="Модификатор доступа: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="property.getMethod.modifier" /> 
		</a-form-item>
		
		<a-form-item v-bind="delBtn">
		<a-popconfirm
          title="Sure to delete?"
          @confirm="property.getMethod = null;"
        >
          <a-button type="danger">Удалить геттер</a-button>
        </a-popconfirm>
		</a-form-item>
  </div>
	 </a-col>
<a-col :span="8">
	  	<div v-if="!property.setMethod">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет сеттера</span>
    <a-button @click="addS" type="primary">Добавить сеттер</a-button>
  </a-empty>
  </div>
  <div v-else>	
  <a-form-item
        label="Область видимости: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="property.setMethod.visibility" /> 
      </a-form-item>
	  <a-form-item
        label="Модификатор доступа: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="property.setMethod.modifier" /> 
		</a-form-item>
		
		<a-form-item v-bind="delBtn">
		<a-popconfirm
          title="Sure to delete?"
          @confirm="property.setMethod = null;"
        >
          <a-button type="danger">Удалить сеттер</a-button>
        </a-popconfirm>
		</a-form-item>
  </div>
	 </a-col>
</a-row>
</a-form>
</a-card>
  </div>`
});

Vue.component('all_properties', {
    props: ['properties'],
    methods: {
        add: function () {
            let last = 0;
            if (this.properties.length > 0) {
                last = this.properties[this.properties.length - 1].key;
            }
            this.properties.push(new PropertyInfo(last + 1));
        }
    },
    template: `
<div class="all_properties"> 
  <div v-if="properties.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет свойств</span>
    <a-button @click="add" type="primary">Добавить свойство</a-button>
  </a-empty>
  </div>
  <div v-else>
  <a-list itemLayout="vertical" size="large" :pagination="{pageSize: 3}" :dataSource="properties">
	<template slot="header">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>Добавить свойство
	</a-button>
	</template>
	<a-list-item slot="renderItem" slot-scope="item, index" key="item.key">
	<property :property="item">
	<a-popconfirm
          title="Sure to delete?"
          @confirm="properties.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm></property>
		</a-list-item>
	</a-list>
</div>
  </div>`
});

Vue.component('event', {
    props: ['eventInfo'],
    data() {
        return {
            formItemLayout: {
                labelCol: {
                    xs: { span: 13 },
                    sm: { span: 13 },
                },
                wrapperCol: {
                    xs: { span: 11 },
                    sm: { span: 11 },
                },
            }
        }
    },
    methods: {
        add: function () {
            let last = 0;
            let pos = 0;
            if (this.eventInfo.parameters.length > 0) {
                pos = this.eventInfo.parameters.length;
                last = this.eventInfo.parameters[pos - 1].key;
            }
            this.eventInfo.parameters.push(new ParameterInfo(pos, last + 1));
        }
    },
    template: `
<div class="event_div"> 
<a-card>
<template slot="title">
		<slot></slot>
<label>{{eventInfo.name}}</label>
</template>
<a-form>
<a-row :gutter="16">
<a-col :span="8">
	<a-form-item
        label="Имя события: "
		v-bind="formItemLayout"
		required
        >
        <a-input allowClear type="text" v-model="eventInfo.name" /> 
      </a-form-item>
	  <a-form-item
        label="Область видимости: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="eventInfo.visibility" /> 
      </a-form-item>
	  <a-form-item
        label="Модификатор доступа: "
		v-bind="formItemLayout"
        >
        <a-input allowClear type="text" v-model="eventInfo.modifier" /> 
		</a-form-item>
		
	  <a-form-item
        label="Возвращаемый тип делегата: "
		v-bind="formItemLayout"
		required
        >
        <a-input allowClear type="text" v-model="eventInfo.returnType" /> 
		</a-form-item>
</a-col>
<a-col :span="16">
		<all_params :params="eventInfo.parameters" :mName="'события ' + eventInfo.name"></all_params>
	 </a-col>
</a-row>
</a-form>
</a-card>
  </div>`
});

Vue.component('all_events', {
    props: ['events'],
    methods: {
        add: function () {
            let last = 0;
            if (this.events.length > 0) {
                last = this.events[this.events.length - 1].key;
            }
            this.events.push(new MethodInfo(last + 1, true));
        }
    },
    template: `
<div class="all_events"> 
  <div v-if="events.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет событий</span>
    <a-button @click="add" type="primary">Добавить событие</a-button>
  </a-empty>
  </div>
  <div v-else>
	<a-list itemLayout="vertical" size="large" :pagination="{pageSize: 3}" :dataSource="events">
	<template slot="header">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>Добавить событие
	</a-button>
	</template>
	<a-list-item slot="renderItem" slot-scope="item, index" key="item.key">
	<event :eventInfo="item">
	<a-popconfirm
          title="Sure to delete?"
          @confirm="events.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm></event>
		</a-list-item>
	</a-list>
</div>
  </div>`
});

Vue.component('test', {
    props: ['method', 'test'],
    data() {
        return {
            formItemLayout: {
                labelCol: {
                    xs: { span: 13 },
                    sm: { span: 13 },
                },
                wrapperCol: {
                    xs: { span: 11 },
                    sm: { span: 11 },
                },
            }
        }
    },
    template: `
<div class="test_div"> 
<a-card>
<template slot="title">
		<slot></slot>
<label>{{test.name}}</label>
</template>
<a-form>
<a-row :gutter="12">
<a-col :span="12">
	<a-form-item
		v-for="(item, index) in method.parameters"
        :label="item.name + ': '"
		:key="item.key"
		v-bind="formItemLayout"
		required
        >
		<div class="param_div"> 
<div v-if="item.type === 'bool'">
	 <a-switch v-model="test.inputs[index]" />
</div>
 <div v-else>
	<div v-if="item.type === 'int'">
		<a-input-number v-model="test.inputs[index]" /> 
	</div>
	<div v-else>
		<a-input allowClear type="text" v-model="test.inputs[index]" />
	</div>
 </div>
</div>
      </a-form-item>
</a-col>
<a-col :span="12">
		<a-form-item
        label="Результат: "
		v-bind="formItemLayout"
		required
        >
        <div class="param_div"> 
<div v-if="method.returnType === 'bool'">
	 <a-switch v-model="test.output" />
</div>
 <div v-else>
	<div v-if="method.returnType === 'int'">
		<a-input-number v-model="test.output" /> 
	</div>
	<div v-else>
		<a-input allowClear type="text" v-model="test.output" />
	</div>
 </div>
</div>
      </a-form-item>
	 </a-col>
</a-row>
</a-form>
</a-card>
  </div>`
});

Vue.component('all_tests', {
    props: ['methods'],
    methods: {
        add: function (key) {
            let sel = this.methods.filter(m => m.key == key)[0];
            let last = 0;
            if (sel.tests.length > 0) {
                last = sel.tests[sel.tests.length - 1].k;
            }
            this.sel.tests.push(new TestInfo(last + 1));
        }
    },
    template: `
<div class="all_tests"> 
  <div v-if="methods.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет методов</span>
  </a-empty>
  </div>
  <div v-else>
  <a-list itemLayout="vertical" size="large" :pagination="{pageSize: 3}" :dataSource="methods">
	<a-list-item v-if="item.returnType != 'void'" slot="renderItem" slot-scope="item, index" key="item.key">
	<method_tests :method="item"></method_tests>
	</a-list-item>
	</a-list>
</div>
  </div>`
});

Vue.component('method_tests', {
    props: ['method'],
    methods: {
        add: function (key) {
            let last = 0;
            if (this.method.tests.length > 0) {
                last = this.method.tests[this.method.tests.length - 1].key;
            }
            this.method.tests.push(new TestInfo(this.method.parameters.length, last + 1));
        }
    },
    template: `
<div class="mtests_div"> 
<a-card>
<template slot="title">
<label>{{method.name}}</label>
<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>Добавить тест
	</a-button>
</template>
<a-list itemLayout="vertical" size="small" :pagination="{pageSize: 3}" :dataSource="method.tests">
	<a-list-item slot="renderItem" slot-scope="item, index" key="item.key">
	<test :test="item" :method="method">
	<a-popconfirm
          title="Sure to delete?"
          @confirm="method.tests.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm></test>
		</a-list-item>
	</a-list>
</a-card>
  </div>`
});

Vue.component('send_to_server', {
    props: ['task', 'isUpdate', 'isLoading'],
    methods: {
        send: function () {
            this.isLoading.isLoading = true;
            var http = new XMLHttpRequest();
            var url = './PostNewTask';
            var data = 'json=' + encodeURIComponent(JSON.stringify(this.task));
            if (this.isUpdate) {
                url = './UpdateTask';
            }
            http.open('POST', url, true);

            //Send the proper header information along with the request
            http.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');

            var that = this;
            http.onreadystatechange = function () {//Call a function when the state changes.
                if (http.readyState == 4 && http.status == 200) {
                    if (http.responseText != 'error') {                        
                        document.location.href = http.responseText;
                    }
                    else {
                        that.isLoading.isLoading = false;
                        that.$message.error('An error occured. Try again!');
                    }
                }
            }           
            http.send(data);
        }
    },
    template: `
<div class="send"> 
<a-popconfirm
          title="Sure to send?"
          @confirm="send"
        >
          <a-button block type="submit">Сохранить</a-button>
        </a-popconfirm>
  </div>`
});

Vue.component('common_tests', {
    props: ['tests'],
    methods: {
        add: function () {
            let last = 0;
            if (this.tests.length > 0) {
                last = this.tests[this.tests.length - 1].key;
            }
            this.tests.push(new CommonTestInfo(last + 1));
        }
    },
    template: `
<div class="common_tests"> 
<div v-if="tests.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
    <span slot="description">Нет тестов</span>
    <a-button @click="add" type="primary">Добавить тест</a-button>
  </a-empty>
  </div>
  <div v-else>
	<a-list itemLayout="vertical" size="large" :pagination="{pageSize: 3}" :dataSource="tests">
	<template slot="header">
	<a-button @click="add" type="primary">
	<a-icon type="plus"></a-icon>Добавить тест
	</a-button>
	</template>
	<a-list-item slot="renderItem" slot-scope="item, index" key="item.key">
	<common_test :test="item">
	<a-popconfirm
          title="Sure to delete?"
          @confirm="tests.splice(index, 1)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm></common_test>
		</a-list-item>
	</a-list>
</div>
  </div>`
});

Vue.component('common_test', {
    props: ['test'],
    template: `
<div class="common_test"> 
<a-card>
<template slot="title">
		<slot></slot>
<label>{{test.name}}</label>
</template>
<a-row :gutter="16">
<a-col :span="12">
	<a-form-item
        label="Название"
        >
        <a-input allowClear type="text" v-model="test.name" /> 
      </a-form-item>
</a-col>
<a-col :span="12">
		<a-form-item
			label="Time Limit (в мс)"
		>
		<a-input-number v-model="test.timeLimit" :min="1" :max="2147483646"/>
		</a-form-item>
</a-col>
</a-row>
<a-row :gutter="16">
<a-col :span="12">
	<label>Ввод:</label>
<a-textarea
      placeholder="Ввод"
	  v-model="test.input"
      :autosize="{ minRows: 5, maxRows: 20 }"
    />
</a-col>
<a-col :span="12">
		<label>Вывод:</label>
<a-textarea
      placeholder="Вывод"
	  v-model="test.output"
      :autosize="{ minRows: 5, maxRows: 20 }"
    />
</a-col>
</a-row>
</a-form>
</a-card>
  </div>`
});

Vue.component('task_tests', {
    props: ['tests', 'test_types', 'isUpdate'],
    data() {
        return {
            columns: [
                {
                    title: 'Название',
                    dataIndex: 'name',
                    scopedSlots: { customRender: 'name' },
                },
                {
                    title: 'Вес',
                    dataIndex: 'weight',
                    scopedSlots: { customRender: 'weight' },
                },
                {
                    title: 'Блокирующий',
                    dataIndex: 'block',
                    scopedSlots: { customRender: 'block' },
                },
                {
                    title: 'Данные',
                    dataIndex: 'data',
                    scopedSlots: { customRender: 'data' },
                },
                {
                    title: 'Данные',
                    dataIndex: 'editData',
                    scopedSlots: { customRender: 'editData' },
                },
                {
                    dataIndex: 'delete',
                    scopedSlots: {
                        customRender: 'delete',
                    },
                    slots: {
                        title: 'addTitle'
                    }
                }
            ],
        };
    },
    methods: {
        add: function (e) {
            let type = this.test_types[0];
            let last = 0;
            if (this.tests.length > 0) {
                last = this.tests[this.tests.length - 1].key;
                type = this.test_types[e.key];
            }
            this.tests.push(new TaskTestInfo(last + 1, type));
            type.disabled = true;
        },
        del: function (index) {
            if (this.isUpdate && !this.tests[index].isNew) {
                var http = new XMLHttpRequest();
                var url = './DeleteTaskTest';
                var data = 'id=' + encodeURIComponent(this.tests[index].id);               
                http.open('POST', url, true);

                //Send the proper header information along with the request
                http.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');

                var that = this;
                http.onreadystatechange = function () {//Call a function when the state changes.
                    if (http.readyState == 4 && http.status == 200) {
                        if (http.responseText != 'error') {
                            document.location.href = http.responseText;
                        }
                        else {
                            that.$message.error('An error occured. Try again!');
                        }
                    }
                }
                http.send(data);
            }
            else {
                this.test_types.filter(m => m.type == this.tests[index].type)[0].disabled = false;
                this.tests.splice(index, 1);
            }
        },
        editData: function (record) {
            window.location.href = './EditTaskTestData?id=' + record.id;
        }
    },
    template: `
<div class="task_tests"> 
  <div v-if="tests.length == 0">
  <a-empty
    image="https://gw.alipayobjects.com/mdn/miniapp_social/afts/img/A*pevERLJC9v0AAAAAAAAAAABjAQAAAQ/original"
  >
	<span slot="description">Нет тестов</span>
    <a-button @click="add" type="primary">Добавить тест</a-button>
  </a-empty>
  </div>
  <div v-else>
    <a-table bordered :dataSource="tests" :columns="columns">
	
	<template slot="addTitle">
	<a-dropdown>
      <a-menu slot="overlay" @click="add">
        <a-menu-item
			v-for="(item, index) in test_types"
			:key="index"
			:disabled="item.disabled"> {{item.name}} </a-menu-item>
      </a-menu>
      <a-button style="margin-left: 8px"> Добавить тест <a-icon type="down" /> </a-button>
    </a-dropdown>
	</template>
	
      <template slot="name" slot-scope="text, record">
	  {{record.name}}
      </template>
	  
	  <template slot="weight" slot-scope="text, record">
        <a-input-number v-model="record.weight"
			:min="0"
			:max="100"		
			:formatter="value => value + '%'"
			:parser="value => value.replace('%', '')"
		/>
      </template>
	  
	  <template slot="block" slot-scope="text, record">
         <a-switch v-model="record.block" />
      </template>
	  	  
	  <template slot="data" slot-scope="text, record">
         {{record.data}}
      </template>

      <template slot="editData" slot-scope="text, record">
         <div v-if="record.isNew">
           <a-button type="dashed" disabled>
            Сначала надо сохранить
            </a-button>
         </div>
        <div v-else>
        <a-popconfirm
          v-if="tests.length"
          title="Несохраненные изменения пропадут! Перейти?"
          @confirm="editData(record)"
        >
        <a-button type="dashed">
            Изменить данные
        </a-button>
        </a-popconfirm>
        </div>
      </template>

	  <template slot="delete" slot-scope="text, record, index">
        <a-popconfirm
          v-if="tests.length"
          title="Sure to delete?"
          @confirm="del(index)"
        >
          <a-button type="danger"><a-icon type="minus"></a-icon></a-button>
        </a-popconfirm>
      </template>
      
    </a-table>
</div>
  </div>`
});

class TaskTestInfo {
    constructor(k, type) {
        this.name = type.name;
        this.type = type.type;
        this.weight = 40;
        this.block = true;
        this.key = k;
        this.id = 0;
        this.isNew = true;
        this.data = "empty";
    }
}

class TypeInfo {
    constructor(k) {
        this.name = 'New Class' + k;
        this.visibility = 'public';
        this.modifier = '';
        this.constructors = [];
        this.fields = [];
        this.properties = [];
        this.methods = [];
        this.events = [];
        this.implementedInterfaces = [];
        this.base = '';
        this.key = k;        
    }
}

class ConstructorInfo {
    constructor(k) {
        this.visibility = 'private';
        this.modifier = '';
        this.parameters = [];
        this.key = k;
    }
}

class FieldInfo {
    constructor(k, isEvent) {
        this.visibility = 'public';
        this.modifier = '';
        if (!isEvent) {
            this.type = 'string';
            this.name = 'NewField' + k;
        } else {
            this.type = 'FormClosedEventHandler';
            this.name = 'NewEvent' + k;
        }
        this.key = k;
    }
}

class PropertyInfo {
    constructor(k) {
        this.type = 'string';
        this.name = 'NewProperty' + k;
        this.getMethod = null;
        this.setMethod = null;
        this.key = k;
    }
}

class MethodInfo {
    constructor(k, isEvent) {
        this.visibility = 'private';
        this.modifier = '';
        this.returnType = isEvent ? 'void' : 'string';
        this.name = (isEvent ? 'NewEvent' : 'NewMethod') + k;
        this.parameters = [];
        this.key = k;
        if (!isEvent) {
            this.tests = [];
        }
    }
}

class ParameterInfo {
    constructor(pos, k) {
        this.type = 'string';
        this.name = 'Parameter' + k;
        this.isOut = false;
        this.defaultValue = '';
        this.position = pos;
        this.key = k;
    }
}

class TestInfo {
    constructor(length, k) {
        this.inputs = new Array(length);
        this.output = '';
        this.key = k;
        this.name = 'Test' + k;
    }
}

class CommonTestInfo {
    constructor(k) {
        this.input = '';
        this.output = '';
        this.key = k;
        this.name = 'Test' + k;
        this.timeLimit = 10000;
    }
}